using System.Text;
using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Channels;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>
/// IKSUNG NFC/RFID 리더기 SDK 의 진입점.
/// 정적 팩토리 메서드(<see cref="ConnectSerialAsync"/>)로 인스턴스를 생성한다.
/// 사용 후 반드시 <see cref="DisposeAsync"/>를 호출하거나 <c>await using</c>으로 감싼다.
/// </summary>
public sealed partial class IksungReader : IAsyncDisposable
{
    private readonly IIksungChannel _channel;
    private bool _disposed;
    private bool _autoReadActive;

    /// <summary>현재 채널 연결 여부.</summary>
    public bool IsConnected => _channel.IsConnected;

    /// <summary>연결된 채널 종류.</summary>
    public ChannelType ConnectedVia { get; }

    /// <summary>
    /// AutoRead 모드에서 카드/태그가 감지될 때 발생한다.
    /// 이벤트 핸들러는 백그라운드 스레드에서 호출되므로 UI 접근 시 디스패치가 필요하다.
    /// </summary>
    public event EventHandler<TagDetectedEventArgs>? TagDetected;

    private IksungReader(IIksungChannel channel, ChannelType type)
    {
        _channel     = channel;
        ConnectedVia = type;
        _channel.PacketReceived += OnChannelPacketReceived;
    }

    // ─── 팩토리 메서드 ────────────────────────────────────────

    /// <summary>
    /// 시리얼 포트로 리더기에 연결한다.
    /// </summary>
    /// <param name="portName">포트 이름 (Windows: "COM3" / Linux: "/dev/ttyUSB0")</param>
    /// <param name="baudRate">통신 속도 (기본값: 115200)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>연결된 <see cref="IksungReader"/> 인스턴스</returns>
    public static async Task<IksungReader> ConnectSerialAsync(
        string portName,
        int baudRate   = 115200,
        CancellationToken ct = default)
    {
        var channel = new SerialIksungChannel(portName, baudRate);
        await channel.ConnectAsync(ct).ConfigureAwait(false);
        return new IksungReader(channel, ChannelType.Serial);
    }

    // ─── Common 명령 ─────────────────────────────────────────

    /// <summary>
    /// 펌웨어 버전 문자열을 읽는다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>버전 문자열 (예: "IS-3500K_V1.8")</returns>
    public async Task<string> ReadVersionAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] resp = await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_VERSION,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return Encoding.ASCII.GetString(resp).TrimEnd('\0').Trim();
    }

    /// <summary>고유 ID (UID)를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadUniqueIdAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>RF 안테나를 켠다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task RfOnAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_COMMON, Constants.COMMON_RFON,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>RF 안테나를 끈다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task RfOffAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_COMMON, Constants.COMMON_RFOFF,
                            timeoutMs: timeoutMs, ct: ct);

    // ─── UID 단순 읽기 ────────────────────────────────────────

    /// <summary>ISO 14443-A 카드 UID를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadIso14443aUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO14443A_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-B 카드 UID를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadIso14443bUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO14443B_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 카드 UID를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadIso15693UidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO15693_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── ISO 14443-A/B 활성화 ────────────────────────────────

    /// <summary>ISO 14443-A 카드를 활성화한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>카드 UID 바이트 배열</returns>
    public async Task<byte[]> ActivateIso14443aAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443A_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-4A (ISO-DEP) 카드를 활성화한다 (106 kbps).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>UID + ATS(Answer to Select) 바이트 배열</returns>
    public async Task<byte[]> ActivateIso14443_4aAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443_4A_106_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-3A + 4A 순서로 카드를 활성화한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso14443_3a4aAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443_3A_4A_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-B 카드를 활성화한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso14443bAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443B_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-A/B 자동 감지 활성화를 시도한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso14443abAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443AB_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-A 카드를 Halt 상태로 전환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task HaltIso14443aAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_ISO14443AB, Constants.ISO14443A_HALT,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>ISO 14443-B 카드를 Halt 상태로 전환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task HaltIso14443bAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_ISO14443AB, Constants.ISO14443B_HALT,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>
    /// ISO 14443-4 (T=CL) APDU 교환.
    /// 카드를 먼저 <see cref="ActivateIso14443_4aAsync"/> 또는 <see cref="ActivateIso14443_3a4aAsync"/>로 활성화해야 한다.
    /// </summary>
    /// <param name="apdu">C-APDU 바이트 배열</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>R-APDU 바이트 배열</returns>
    public async Task<byte[]> ExchangeApduAsync(byte[] apdu, int timeoutMs = 3000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443P4_DATA_EXCHANGE,
            data: apdu, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Mifare Classic ──────────────────────────────────────

    /// <summary>Mifare Classic 카드를 활성화하고 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateMifareAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE, Constants.MIFARE_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 블록 인증.</summary>
    /// <param name="blockNo">인증할 블록 번호 (0–255)</param>
    /// <param name="keyType"><see cref="MifareKeyType.KeyA"/> 또는 <see cref="MifareKeyType.KeyB"/></param>
    /// <param name="key">6바이트 인증 키</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task MifareAuthenticateAsync(
        byte blockNo, MifareKeyType keyType, byte[] key,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (key.Length != 6) throw new ArgumentException("Mifare 키는 6바이트여야 합니다.", nameof(key));
        byte[] data = new byte[8];
        data[0] = blockNo;
        data[1] = (byte)keyType;
        key.CopyTo(data, 2);
        await SendCommandAsync(Constants.MAJOR_MIFARE, Constants.MIFARE_AUTHENTICATE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 블록(16바이트)을 읽는다.</summary>
    /// <param name="blockNo">읽을 블록 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>16바이트 블록 데이터</returns>
    public async Task<byte[]> MifareReadBlockAsync(byte blockNo, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE, Constants.MIFARE_BLOCK_READ,
            data: [blockNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 블록(16바이트)을 쓴다.</summary>
    /// <param name="blockNo">쓸 블록 번호</param>
    /// <param name="data">16바이트 블록 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task MifareWriteBlockAsync(byte blockNo, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data.Length != 16) throw new ArgumentException("Mifare 블록 데이터는 16바이트여야 합니다.", nameof(data));
        byte[] payload = new byte[17];
        payload[0] = blockNo;
        data.CopyTo(payload, 1);
        await SendCommandAsync(Constants.MAJOR_MIFARE, Constants.MIFARE_BLOCK_WRITE,
            data: payload, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 섹터(64바이트) 전체를 읽는다.</summary>
    /// <param name="sectorNo">섹터 번호 (0–15)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> MifareReadSectorAsync(byte sectorNo, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE, Constants.MIFARE_SECTOR_READ,
            data: [sectorNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Mifare Ultralight / NTag ────────────────────────────

    /// <summary>Mifare Ultralight / NTag 카드를 활성화하고 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateMifareUltralightAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.ULC_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Ultralight 페이지(4바이트)를 읽는다.</summary>
    /// <param name="page">읽을 페이지 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>4바이트 페이지 데이터</returns>
    public async Task<byte[]> MifareUltralightReadPageAsync(byte page, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.ULC_BLOCK_READ,
            data: [page], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Ultralight 페이지(4바이트)를 쓴다.</summary>
    /// <param name="page">쓸 페이지 번호</param>
    /// <param name="data">4바이트 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task MifareUltralightWritePageAsync(byte page, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data.Length != 4) throw new ArgumentException("Ultralight 페이지 데이터는 4바이트여야 합니다.", nameof(data));
        byte[] payload = new byte[5];
        payload[0] = page;
        data.CopyTo(payload, 1);
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.ULC_BLOCK_WRITE,
            data: payload, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag 패스워드 인증.</summary>
    /// <param name="password">4바이트 패스워드</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task NTagPasswordAuthAsync(byte[] password, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (password.Length != 4) throw new ArgumentException("NTag 패스워드는 4바이트여야 합니다.", nameof(password));
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_PASSWORD_AUTH,
            data: password, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── ISO 15693 ────────────────────────────────────────────

    /// <summary>ISO 15693 카드를 활성화하고 8바이트 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso15693Async(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO15693, Constants.ISO15693_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 단일 블록을 읽는다.</summary>
    /// <param name="blockNo">읽을 블록 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> Iso15693ReadBlockAsync(byte blockNo, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO15693, Constants.ISO15693_SINGLE_BLOCK_READ,
            data: [blockNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 단일 블록을 쓴다.</summary>
    /// <param name="blockNo">쓸 블록 번호</param>
    /// <param name="data">블록 데이터 (카드에 따라 4 또는 8바이트)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task Iso15693WriteBlockAsync(byte blockNo, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] payload = new byte[1 + data.Length];
        payload[0] = blockNo;
        data.CopyTo(payload, 1);
        await SendCommandAsync(Constants.MAJOR_ISO15693, Constants.ISO15693_SINGLE_BLOCK_WRITE,
            data: payload, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 다중 블록을 읽는다.</summary>
    /// <param name="firstBlock">첫 번째 블록 번호</param>
    /// <param name="blockCount">읽을 블록 수</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> Iso15693ReadMultipleBlocksAsync(byte firstBlock, byte blockCount, int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO15693, Constants.ISO15693_MULTIPLE_BLOCK_READ,
            data: [firstBlock, blockCount], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── LF 125 kHz ──────────────────────────────────────────

    /// <summary>LF 125 kHz (EM410X) 카드 UID를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLf125KhzUidAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_EM410X_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>LF 125 kHz 고유 ID를 읽는다 (EM410X 이외 형식 포함).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLf125KhzRawUidAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── AutoRead 모드 ────────────────────────────────────────

    /// <summary>
    /// AutoRead 폴링을 시작한다.
    /// 카드가 감지될 때마다 <see cref="TagDetected"/> 이벤트가 발생한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task StartAutoReadAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_AUTO, Constants.AUTOSETUP_POLLING_START,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        _autoReadActive = true;
    }

    /// <summary>AutoRead 폴링을 중지한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task StopAutoReadAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _autoReadActive = false;
        await SendCommandAsync(
            Constants.MAJOR_AUTO, Constants.AUTOSETUP_POLLING_STOP,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Raw command ─────────────────────────────────────────

    /// <summary>
    /// 전문가용: 임의의 raw 명령을 전송하고 응답 Data 페이로드를 반환한다.
    /// </summary>
    /// <param name="cmd1">Major 커맨드 바이트</param>
    /// <param name="cmd2">Minor 커맨드 바이트</param>
    /// <param name="data">요청 데이터 (선택)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task<byte[]> SendRawCommandAsync(
        byte cmd1, byte cmd2,
        byte[]? data    = null,
        int timeoutMs   = 1000,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return SendCommandAsync(cmd1, cmd2, data, timeoutMs, ct);
    }

    // ─── Dispose ─────────────────────────────────────────────

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.PacketReceived -= OnChannelPacketReceived;
        await Task.Run(_channel.Disconnect).ConfigureAwait(false);
        _channel.Dispose();
    }

    // ─── Internal helpers ────────────────────────────────────

    private async Task<byte[]> SendCommandAsync(
        byte cmd1, byte cmd2,
        byte[]? data    = null,
        int timeoutMs   = 1000,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (!_channel.IsConnected)
            throw new ChannelDisconnectedException("리더기와 연결되어 있지 않습니다.");
        byte[] packet = PacketBuilder.BuildPacket(cmd1, cmd2, data);
        return await _channel.SendAndReceiveAsync(packet, timeoutMs, ct).ConfigureAwait(false);
    }

    private void OnChannelPacketReceived(object? sender, IksungPacket pkt)
    {
        if (!_autoReadActive) return;
        if (pkt.Cmd1 != Constants.MAJOR_AUTO) return;

        var (cardType, uid) = MapAutoReadPacket(pkt);
        TagDetected?.Invoke(this, new TagDetectedEventArgs(cardType, uid, pkt.Cmd1, pkt.Cmd2, pkt.Data));
    }

    private static (CardType, byte[]) MapAutoReadPacket(IksungPacket pkt)
    {
        byte cmd2 = (byte)(pkt.Cmd2 & ~Constants.BUZZER_FLAG);
        return cmd2 switch
        {
            Constants.AUTOSETUP_ISO14443A_UID         => (CardType.Iso14443a,       pkt.Data),
            Constants.AUTOSETUP_ISO14443B_UID         => (CardType.Iso14443b,       pkt.Data),
            Constants.AUTOSETUP_ISO14443A_MIFARE1 or
            Constants.AUTOSETUP_ISO14443A_MIFARE2 or
            Constants.AUTOSETUP_ISO14443A_MIFARE3 or
            Constants.AUTOSETUP_ISO14443A_MIFARE4     => (CardType.MifareClassic,   pkt.Data),
            Constants.AUTOSETUP_MIFARE_UL or
            Constants.AUTOSETUP_MIFARE_NTAG           => (CardType.MifareUltralight, pkt.Data),
            Constants.AUTOSETUP_ISO15693_UID          => (CardType.Iso15693,        pkt.Data),
            Constants.AUTOSETUP_FELICA_UID            => (CardType.Felica,          pkt.Data),
            Constants.AUTOSETUP_LF_EM_UID or
            Constants.AUTOSETUP_LF_SECOM_LONGDATA_UID => (CardType.Lf125Khz,       pkt.Data),
            _                                         => (CardType.Unknown,         pkt.Data),
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IksungReader));
    }
}
