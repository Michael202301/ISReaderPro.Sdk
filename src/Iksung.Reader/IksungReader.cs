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
public sealed class IksungReader : IAsyncDisposable
{
    private readonly IIksungChannel _channel;
    private bool _disposed;

    /// <summary>현재 채널 연결 여부.</summary>
    public bool IsConnected => _channel.IsConnected;

    /// <summary>연결된 채널 종류.</summary>
    public ChannelType ConnectedVia { get; }

    private IksungReader(IIksungChannel channel, ChannelType type)
    {
        _channel     = channel;
        ConnectedVia = type;
    }

    // ─── 팩토리 메서드 ────────────────────────────────────────

    /// <summary>
    /// 시리얼 포트로 리더기에 연결한다.
    /// </summary>
    /// <param name="portName">포트 이름 (Windows: "COM3" / Linux: "/dev/ttyUSB0" / Mac: "/dev/tty.usbserial-XXXX")</param>
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

    /// <summary>펌웨어 버전 문자열을 읽는다.</summary>
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
    public async Task<byte[]> ReadUniqueIdAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>RF 안테나를 켠다.</summary>
    public Task RfOnAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_COMMON, Constants.COMMON_RFON,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>RF 안테나를 끈다.</summary>
    public Task RfOffAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_COMMON, Constants.COMMON_RFOFF,
                            timeoutMs: timeoutMs, ct: ct);

    // ─── ISO14443A UID ────────────────────────────────────────

    /// <summary>ISO 14443-A 카드 UID를 읽는다.</summary>
    public async Task<byte[]> ReadIso14443aUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO14443A_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-B 카드 UID를 읽는다.</summary>
    public async Task<byte[]> ReadIso14443bUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO14443B_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 카드 UID를 읽는다.</summary>
    public async Task<byte[]> ReadIso15693UidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO15693_UID_READ,
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

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IksungReader));
    }
}
