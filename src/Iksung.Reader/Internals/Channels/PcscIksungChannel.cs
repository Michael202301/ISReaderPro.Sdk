using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Protocol;
using System.Runtime.InteropServices;

namespace Iksung.Reader.Internals.Channels;

/// <summary>
/// PC/SC (CCID) 채널 — winscard.dll 직접 P/Invoke 로 SCardConnect/Transmit/Disconnect 구현.
/// STX/ETX 시리얼 패킷 ↔ ISO 7816-4 APDU 양방향 변환 포함.
/// Reference: ISReaderPro V6.01 PcscService.cs (V6.05 기준 검증 완료).
/// </summary>
internal sealed class PcscIksungChannel : IIksungChannel
{
    private readonly string _readerName;
    private IntPtr _context    = IntPtr.Zero;
    private IntPtr _cardHandle = IntPtr.Zero;
    private int    _activeProtocol;

    // Raw I/O 상태 (펌웨어 업데이트 V6.0/V7.0 용)
    private bool   _rawIoActive;
    private byte[] _rawRxBuffer  = Array.Empty<byte>();
    private int    _rawRxConsumed;

    // 리더 모니터 (1초 polling)
    private readonly CancellationTokenSource _monitorCts = new();

    public bool IsConnected { get; private set; }

    /// <summary>SCardConnect 직후 SCardStatusW 로 조회한 ATR.</summary>
    public byte[]? CurrentAtr { get; private set; }

    // PC/SC 는 push 수신이 없으므로 PacketReceived 는 발사되지 않지만 IIksungChannel 계약상 선언 필요.
#pragma warning disable CS0067
    public event EventHandler<IksungPacket>? PacketReceived;
#pragma warning restore CS0067
    public event EventHandler<bool>?         ConnectionChanged;
    public event EventHandler<byte[]>?       RawDataReceived;

    public PcscIksungChannel(string readerName)
    {
        _readerName = readerName;
    }

    // ─── Connection ────────────────────────────────────────────

    public Task<bool> ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) Disconnect();

        // 1) SCardEstablishContext
        int rc = SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out _context);
        if (rc != SCARD_S_SUCCESS) { _context = IntPtr.Zero; return Task.FromResult(false); }

        // 2) SCardConnect — T=0 or T=1 자동 협상
        rc = SCardConnect(_context, _readerName, SCARD_SHARE_SHARED,
                          SCARD_PROTOCOL_T0 | SCARD_PROTOCOL_T1,
                          out _cardHandle, out _activeProtocol);
        if (rc != SCARD_S_SUCCESS)
        {
            try { SCardReleaseContext(_context); } catch { }
            _context = _cardHandle = IntPtr.Zero;
            return Task.FromResult(false);
        }

        // ATR 캐싱 (SCardConnect 직후 카드 active 상태에서 조회)
        CurrentAtr = FetchAtr();
        SetConnected(true);
        StartReaderMonitor();
        return Task.FromResult(true);
    }

    public void Disconnect()
    {
        StopReaderMonitor();
        if (_cardHandle != IntPtr.Zero)
        {
            try { SCardDisconnect(_cardHandle, SCARD_LEAVE_CARD); } catch { }
            _cardHandle = IntPtr.Zero;
        }
        if (_context != IntPtr.Zero)
        {
            try { SCardReleaseContext(_context); } catch { }
            _context = IntPtr.Zero;
        }
        CurrentAtr = null;
        SetConnected(false);
    }

    public async Task<bool> ReconnectAsync(CancellationToken ct = default)
    {
        Disconnect();
        await Task.Delay(200, ct).ConfigureAwait(false);
        return await ConnectAsync(ct).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _monitorCts.Cancel();
        _monitorCts.Dispose();
        Disconnect();
    }

    // ─── Send / Receive ────────────────────────────────────────

    /// <summary>
    /// Fire-and-forget 송신 — PC/SC 는 항상 응답이 있으므로 이 경로는 사용하지 않는다.
    /// SendAndReceiveAsync 를 사용할 것.
    /// </summary>
    public void Send(byte[] data)
    {
        // PC/SC 는 동기 request-response 만 지원. 무시하거나 로깅용으로만 사용.
        // 실제 명령 전송은 SendAndReceiveAsync 를 통해서만 이루어진다.
    }

    /// <summary>
    /// STX 패킷을 ISO 7816 APDU 로 변환하여 SCardTransmit 호출 후 응답 데이터를 반환한다.
    /// SW ≠ 90 00 이면 IksungProtocolException (STATE_FAIL) 을 던진다.
    /// </summary>
    public Task<byte[]> SendAndReceiveAsync(byte[] request, int timeoutMs, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (!IsConnected || _cardHandle == IntPtr.Zero)
                throw new ChannelDisconnectedException("PC/SC 리더가 연결되지 않았습니다.");
            if (request == null || request.Length < 7 || request[0] != STX)
                throw new IksungProtocolException("유효하지 않은 STX 패킷입니다.");

            // STX 패킷 파싱: STX CMD1 CMD2 LEN_H LEN_L Data CS ETX
            byte cmd1    = request[1];
            byte cmd2Raw = request[2];
            int  dataLen = (request[3] << 8) | request[4];
            var  data    = new byte[dataLen];
            if (dataLen > 0) Array.Copy(request, 5, data, 0, dataLen);

            // STX → APDU 변환 (ISO 7816-4 4-Case 규칙)
            byte[] apdu = BuildApdu(cmd1, cmd2Raw, data);

            // RawDataReceived: TX (LogRawPackets 옵션용)
            RawDataReceived?.Invoke(this, apdu);

            // SCardTransmit
            byte[]? response = TransmitApdu(apdu);
            if (response == null || response.Length < 2)
                throw new IksungProtocolException("SCardTransmit 응답 없음");

            // RawDataReceived: RX
            RawDataReceived?.Invoke(this, response);

            ct.ThrowIfCancellationRequested();

            // 마지막 2바이트 = SW1 SW2
            byte sw1 = response[response.Length - 2];
            byte sw2 = response[response.Length - 1];

            if (sw1 != 0x90 || sw2 != 0x00)
                throw new IksungProtocolException(
                    $"PC/SC 명령 실패 (SW={sw1:X2}{sw2:X2})");

            // SW 제거, Data 부분만 반환
            var result = new byte[response.Length - 2];
            if (result.Length > 0) Array.Copy(response, result, result.Length);
            return result;
        }, ct);
    }

    // ─── Raw I/O (펌웨어 업데이트 V6.0/V7.0) ──────────────────

    public IDisposable BeginRawIo()
    {
        if (!IsConnected || _cardHandle == IntPtr.Zero)
            throw new InvalidOperationException("PC/SC 리더가 연결되지 않았습니다.");
        _rawIoActive   = true;
        _rawRxBuffer   = Array.Empty<byte>();
        _rawRxConsumed = 0;
        return new RawIoLease(this);
    }

    public bool WriteRaw(byte[] data, int offset, int count)
    {
        if (!_rawIoActive || !IsConnected || _cardHandle == IntPtr.Zero) return false;
        if (data == null || count < 7) return false;
        if (data[offset] != STX) return false;

        byte cmd1    = data[offset + 1];
        byte cmd2    = data[offset + 2];
        int  dataLen = (data[offset + 3] << 8) | data[offset + 4];
        if (count < 7 + dataLen) return false;

        var payload = new byte[dataLen];
        if (dataLen > 0) Array.Copy(data, offset + 5, payload, 0, dataLen);

        byte[] apdu = BuildBootloaderApdu(cmd1, cmd2, payload, requestResponseData: true);
        byte[]? response = TransmitApdu(apdu, 1024 + 2);
        if (response == null || response.Length < 2) return false;

        byte sw1   = response[response.Length - 2];
        byte sw2   = response[response.Length - 1];
        byte state = (sw1 == 0x90 && sw2 == 0x00) ? STATE_SUCCESS : STATE_FAIL;

        int  respDataLen = response.Length - 2;
        var frame = new byte[8 + respDataLen];
        frame[0] = STX;
        frame[1] = cmd1;
        frame[2] = cmd2;
        frame[3] = state;
        frame[4] = (byte)(respDataLen >> 8);
        frame[5] = (byte)(respDataLen & 0xFF);
        if (respDataLen > 0) Array.Copy(response, 0, frame, 6, respDataLen);

        byte cs = 0;
        for (int i = 1; i < 6 + respDataLen; i++) cs += frame[i];
        frame[6 + respDataLen] = cs;
        frame[7 + respDataLen] = ETX;

        _rawRxBuffer   = frame;
        _rawRxConsumed = 0;
        return true;
    }

    public int ReadRaw(byte[] buf, int offset, int count, int timeoutMs)
    {
        if (!_rawIoActive) return 0;
        int available = _rawRxBuffer.Length - _rawRxConsumed;
        if (available <= 0) return 0;
        int n = Math.Min(available, count);
        Array.Copy(_rawRxBuffer, _rawRxConsumed, buf, offset, n);
        _rawRxConsumed += n;
        return n;
    }

    public void SetBaudRate(int baud) { /* PC/SC 는 baud 개념 없음 — no-op */ }

    // ─── Private helpers ───────────────────────────────────────

    private byte[]? FetchAtr()
    {
        if (_cardHandle == IntPtr.Zero) return null;
        try
        {
            var sb        = new System.Text.StringBuilder(256);
            int readerLen = sb.Capacity;
            var atrBuf    = new byte[33];
            int atrLen    = atrBuf.Length;
            int rc = SCardStatus(_cardHandle, sb, ref readerLen,
                                 out _, out _, atrBuf, ref atrLen);
            if (rc != SCARD_S_SUCCESS || atrLen <= 0) return null;
            var result = new byte[atrLen];
            Array.Copy(atrBuf, result, atrLen);
            return result;
        }
        catch { return null; }
    }

    /// <summary>
    /// STX → APDU 변환 (ISO 7816-4 4-Case 규칙).
    /// data 없음 → Case 2 Short (FF CMD1 CMD2 00 00), Le=0x00 으로 최대 256byte 응답 기대.
    /// data 있음 → Case 3 Short (FF CMD1 CMD2 00 Lc Data), SW 만 응답.
    /// </summary>
    private static byte[] BuildApdu(byte cmd1, byte cmd2Raw, byte[] data)
    {
        if (data.Length == 0)
            return new byte[] { 0xFF, cmd1, cmd2Raw, 0x00, 0x00 };

        var apdu = new byte[5 + data.Length];
        apdu[0] = 0xFF;
        apdu[1] = cmd1;
        apdu[2] = cmd2Raw;           // buzzer 비트 보존
        apdu[3] = 0x00;
        apdu[4] = (byte)data.Length; // Lc
        Array.Copy(data, 0, apdu, 5, data.Length);
        return apdu;
    }

    private byte[]? TransmitApdu(byte[] apdu, int responseBufferSize = 258)
    {
        var response    = new byte[responseBufferSize];
        int responseLen = response.Length;
        var ioReq = new SCARD_IO_REQUEST
        {
            dwProtocol  = _activeProtocol,
            cbPciLength = Marshal.SizeOf<SCARD_IO_REQUEST>(),
        };
        IntPtr ioReqPtr = Marshal.AllocHGlobal(ioReq.cbPciLength);
        try
        {
            Marshal.StructureToPtr(ioReq, ioReqPtr, false);
            int rc = SCardTransmit(_cardHandle, ioReqPtr, apdu, apdu.Length,
                                   IntPtr.Zero, response, ref responseLen);
            if (rc != SCARD_S_SUCCESS) return null;
            var result = new byte[responseLen];
            Array.Copy(response, result, responseLen);
            return result;
        }
        finally { Marshal.FreeHGlobal(ioReqPtr); }
    }

    /// <summary>V6.0/V7.0 부트로더용 Extended/Short APDU 빌더.</summary>
    private static byte[] BuildBootloaderApdu(byte cmd1, byte cmd2, byte[] data, bool requestResponseData)
    {
        int dataLen = data.Length;
        if (dataLen <= 255)
        {
            int extra = (dataLen > 0 ? 1 + dataLen : 0) + (requestResponseData ? 1 : 0);
            var apdu  = new byte[4 + extra];
            apdu[0] = 0xFF; apdu[1] = cmd1; apdu[2] = cmd2; apdu[3] = 0x00;
            int idx = 4;
            if (dataLen > 0) { apdu[idx++] = (byte)dataLen; Array.Copy(data, 0, apdu, idx, dataLen); idx += dataLen; }
            if (requestResponseData) apdu[idx] = 0x00;
            return apdu;
        }
        else
        {
            int extra = 3 + dataLen + (requestResponseData ? 2 : 0);
            var apdu  = new byte[4 + extra];
            apdu[0] = 0xFF; apdu[1] = cmd1; apdu[2] = cmd2; apdu[3] = 0x00;
            apdu[4] = 0x00; apdu[5] = (byte)(dataLen >> 8); apdu[6] = (byte)(dataLen & 0xFF);
            Array.Copy(data, 0, apdu, 7, dataLen);
            if (requestResponseData) { apdu[7 + dataLen] = 0x00; apdu[7 + dataLen + 1] = 0x00; }
            return apdu;
        }
    }

    private void SetConnected(bool value)
    {
        if (IsConnected == value) return;
        IsConnected = value;
        ConnectionChanged?.Invoke(this, value);
    }

    // ─── Reader monitor (1초 polling — 연결 끊김 감지) ─────────

    private Task? _monitorTask;

    private void StartReaderMonitor()
    {
        var ct = _monitorCts.Token;
        _monitorTask = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                    if (!IsConnected) continue;

                    // 리더 목록에서 현재 리더가 사라졌는지 확인
                    var readers = IksungPcscDiscovery.GetAvailableReaders();
                    if (!readers.Contains(_readerName))
                    {
                        SetConnected(false);
                        return;
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, ct);
    }

    private void StopReaderMonitor()
    {
        try { _monitorCts.Cancel(); } catch { }
    }

    // ─── RawIoLease ────────────────────────────────────────────

    private sealed class RawIoLease : IDisposable
    {
        private readonly PcscIksungChannel _owner;
        private bool _disposed;
        public RawIoLease(PcscIksungChannel owner) => _owner = owner;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner._rawIoActive   = false;
            _owner._rawRxBuffer   = Array.Empty<byte>();
            _owner._rawRxConsumed = 0;
        }
    }

    // ─── Protocol constants ────────────────────────────────────
    private const byte STX          = 0x01;
    private const byte ETX          = 0x03;
    private const byte STATE_SUCCESS = 0x01;
    private const byte STATE_FAIL    = 0xFF;

    // ─── winscard.dll P/Invoke (V6.01 PcscService.cs より) ────
    private const int SCARD_SCOPE_USER             = 0;
    private const int SCARD_SHARE_SHARED           = 2;
    private const int SCARD_PROTOCOL_T0            = 1;
    private const int SCARD_PROTOCOL_T1            = 2;
    private const int SCARD_LEAVE_CARD             = 0;
    private const int SCARD_S_SUCCESS              = 0;
    private const int SCARD_E_NO_READERS_AVAILABLE = unchecked((int)0x8010002E);
    private const int SCARD_E_NO_SERVICE           = unchecked((int)0x8010001D);
    private const int SCARD_E_SERVICE_STOPPED      = unchecked((int)0x80100007);

    [StructLayout(LayoutKind.Sequential)]
    private struct SCARD_IO_REQUEST
    {
        public int dwProtocol;
        public int cbPciLength;
    }

    [DllImport("winscard.dll", SetLastError = true)]
    private static extern int SCardEstablishContext(int dwScope, IntPtr pvReserved1, IntPtr pvReserved2, out IntPtr phContext);

    [DllImport("winscard.dll", EntryPoint = "SCardListReadersW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SCardListReaders(IntPtr hContext, byte[]? mszGroups, IntPtr mszReaders, ref int pcchReaders);

    [DllImport("winscard.dll", SetLastError = true)]
    private static extern int SCardReleaseContext(IntPtr hContext);

    [DllImport("winscard.dll", EntryPoint = "SCardConnectW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SCardConnect(IntPtr hContext, string szReader, int dwShareMode,
                                           int dwPreferredProtocols, out IntPtr phCard, out int pdwActiveProtocol);

    [DllImport("winscard.dll", SetLastError = true)]
    private static extern int SCardDisconnect(IntPtr hCard, int dwDisposition);

    [DllImport("winscard.dll", SetLastError = true)]
    private static extern int SCardTransmit(IntPtr hCard, IntPtr pioSendPci,
                                            byte[] pbSendBuffer, int cbSendLength,
                                            IntPtr pioRecvPci, byte[] pbRecvBuffer, ref int pcbRecvLength);

    [DllImport("winscard.dll", EntryPoint = "SCardStatusW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SCardStatus(IntPtr hCard, System.Text.StringBuilder szReaderName, ref int pcchReaderLen,
                                          out int pdwState, out int pdwProtocol, byte[] pbAtr, ref int pcbAtrLen);

    internal static IReadOnlyList<string> ListReaders()
    {
        var result = new List<string>();
        int rc = SCardEstablishContext(SCARD_SCOPE_USER, IntPtr.Zero, IntPtr.Zero, out IntPtr ctx);
        if (rc == SCARD_E_NO_SERVICE || rc == SCARD_E_SERVICE_STOPPED || rc != SCARD_S_SUCCESS)
            return result;
        try
        {
            int charsNeeded = 0;
            rc = SCardListReaders(ctx, null, IntPtr.Zero, ref charsNeeded);
            if (rc == SCARD_E_NO_READERS_AVAILABLE || charsNeeded <= 0) return result;
            if (rc != SCARD_S_SUCCESS) return result;

            IntPtr buffer = Marshal.AllocHGlobal(charsNeeded * 2);
            try
            {
                rc = SCardListReaders(ctx, null, buffer, ref charsNeeded);
                if (rc != SCARD_S_SUCCESS) return result;
                int offset = 0;
                while (offset < charsNeeded)
                {
                    string? s = Marshal.PtrToStringUni(buffer + offset * 2);
                    if (string.IsNullOrEmpty(s)) break;
                    result.Add(s);
                    offset += s.Length + 1;
                }
                return result;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        finally { SCardReleaseContext(ctx); }
    }
}
