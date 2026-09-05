using Iksung.Reader.Exceptions;
using PCSC;

namespace Iksung.Reader.Internals.Channels;

/// <summary>
/// PC/SC (CCID) 채널 — PCSC NuGet(pcsc-sharp) 으로 Connect/Transmit/Disconnect 구현.
/// 네이티브 백엔드는 OS 별로 자동 선택된다(Windows=winscard.dll / Linux=libpcsclite / macOS=PCSC.framework).
/// STX/ETX 시리얼 패킷 ↔ ISO 7816-4 APDU 양방향 변환 포함.
/// Reference: ISReaderPro V6.01 PcscService.cs (V6.05 검증). winscard.dll 직접 P/Invoke 에서 이관(cross-platform).
/// </summary>
internal sealed class PcscIksungChannel : IIksungChannel
{
    private readonly string _readerName;
    private ISCardContext? _context;
    private ICardReader?   _reader;

    // Raw I/O 상태 (펌웨어 업데이트 V6.0/V7.0 용)
    private bool   _rawIoActive;
    private byte[] _rawRxBuffer  = Array.Empty<byte>();
    private int    _rawRxConsumed;

    // 리더 모니터 (1초 polling)
    private readonly CancellationTokenSource _monitorCts = new();

    public bool IsConnected { get; private set; }

    /// <summary>연결 직후 ATR (SCardAttribute.AtrString) 으로 조회한 값.</summary>
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

        // 1) PC/SC 컨텍스트 확립. 서비스 미실행/미설치(Windows SCardSvr 중지 /
        //    Linux pcscd 부재 / macOS PCSC.framework 부재) 시 예외 → false.
        try
        {
            _context = ContextFactory.Instance.Establish(SCardScope.User);
        }
        catch (Exception)
        {
            _context = null;
            return Task.FromResult(false);
        }

        // 2) 리더 연결 — T=0 / T=1 자동 협상. 카드 없음/사용 중 등은 예외 → false.
        try
        {
            _reader = _context.ConnectReader(_readerName, SCardShareMode.Shared,
                                             SCardProtocol.T0 | SCardProtocol.T1);
        }
        catch (Exception)
        {
            try { _context.Dispose(); } catch { }
            _context = null;
            _reader  = null;
            return Task.FromResult(false);
        }

        // ATR 캐싱 (연결 직후 카드 active 상태에서 조회)
        CurrentAtr = FetchAtr();
        SetConnected(true);
        StartReaderMonitor();
        return Task.FromResult(true);
    }

    public void Disconnect()
    {
        StopReaderMonitor();
        if (_reader != null)
        {
            try { _reader.Disconnect(SCardReaderDisposition.Leave); } catch { }
            try { _reader.Dispose(); } catch { }
            _reader = null;
        }
        if (_context != null)
        {
            try { _context.Dispose(); } catch { }   // Dispose 가 SCardReleaseContext 수행
            _context = null;
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
    /// STX 패킷을 ISO 7816 APDU 로 변환하여 Transmit 호출 후 응답 데이터를 반환한다.
    /// SW ≠ 90 00 이면 IksungProtocolException (STATE_FAIL) 을 던진다.
    /// </summary>
    public Task<byte[]> SendAndReceiveAsync(byte[] request, int timeoutMs, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (!IsConnected || _reader == null)
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

            // Transmit (협상된 프로토콜 PCI 자동 사용)
            byte[]? response = TransmitApdu(apdu);
            if (response == null || response.Length < 2)
                throw new IksungProtocolException("PC/SC Transmit 응답 없음");

            // RawDataReceived: RX
            RawDataReceived?.Invoke(this, response);

            ct.ThrowIfCancellationRequested();

            // 마지막 2바이트 = SW1 SW2
            byte sw1 = response[response.Length - 2];
            byte sw2 = response[response.Length - 1];

            if (sw1 != 0x90 || sw2 != 0x00)
            {
                // SW=6300 은 실측상 대부분 "안테나에 태그가 없음" 이다(리더 고장이 아니다).
                // 첫 사용자가 SDK 오류로 오해하지 않도록 원인을 함께 알려준다.
                string hint = (sw1 == 0x63 && sw2 == 0x00)
                    ? " — 안테나에 태그가 없거나 명령이 거부되었습니다. 카드를 리더에 올려둔 채 다시 시도하세요."
                    : string.Empty;
                throw new IksungProtocolException(
                    $"PC/SC 명령 실패 (SW={sw1:X2}{sw2:X2}){hint}");
            }

            // SW 제거, Data 부분만 반환
            var result = new byte[response.Length - 2];
            if (result.Length > 0) Array.Copy(response, result, result.Length);
            return result;
        }, ct);
    }

    // ─── Raw I/O (펌웨어 업데이트 V6.0/V7.0) ──────────────────

    public IDisposable BeginRawIo()
    {
        if (!IsConnected || _reader == null)
            throw new InvalidOperationException("PC/SC 리더가 연결되지 않았습니다.");
        _rawIoActive   = true;
        _rawRxBuffer   = Array.Empty<byte>();
        _rawRxConsumed = 0;
        return new RawIoLease(this);
    }

    public bool WriteRaw(byte[] data, int offset, int count)
    {
        if (!_rawIoActive || !IsConnected || _reader == null) return false;
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
        var reader = _reader;
        if (reader == null) return null;
        try
        {
            byte[] atr = reader.GetAttrib(SCardAttribute.AtrString);
            if (atr == null || atr.Length == 0) return null;
            return atr;
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

    /// <summary>
    /// APDU 를 Transmit 하고 응답 바이트(SW 포함)를 반환한다. 실패/예외 시 null.
    /// pcsc-sharp 의 Transmit(byte[], byte[]) 는 협상된 프로토콜의 PCI 를 자동 사용하고
    /// 수신 바이트 수를 반환한다.
    /// </summary>
    private byte[]? TransmitApdu(byte[] apdu, int responseBufferSize = 258)
    {
        var reader = _reader;
        if (reader == null) return null;
        try
        {
            var response = new byte[responseBufferSize];
            int received = reader.Transmit(apdu, response);
            if (received < 2) return null;          // 최소 SW1 SW2
            var result = new byte[received];
            Array.Copy(response, result, received);
            return result;
        }
        catch { return null; }
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
    private const byte STX           = 0x01;
    private const byte ETX           = 0x03;
    private const byte STATE_SUCCESS = 0x01;
    private const byte STATE_FAIL    = 0xFF;

    // ─── PC/SC reader enumeration (PCSC NuGet — cross-platform) ─

    /// <summary>
    /// 현재 시스템에 연결된 PC/SC 리더 이름 목록을 반환한다.
    /// PC/SC 서비스 미실행/미설치 또는 리더 없음(NoReadersAvailable) 시 빈 목록을 반환한다.
    /// </summary>
    internal static IReadOnlyList<string> ListReaders()
    {
        try
        {
            using var ctx = ContextFactory.Instance.Establish(SCardScope.User);
            string[] readers = ctx.GetReaders();
            return readers ?? Array.Empty<string>();
        }
        catch (Exception)
        {
            // Windows SCardSvr 중지 / Linux pcscd 부재 / macOS PCSC.framework 부재
            // 또는 리더 없음(NoReadersAvailable) → 빈 목록.
            return Array.Empty<string>();
        }
    }
}
