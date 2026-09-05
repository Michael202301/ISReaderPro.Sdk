using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Channels;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>
/// IKSUNG NFC/RFID 리더기 SDK 의 진입점.
/// 정적 팩토리 메서드(<c>ConnectSerialAsync</c>)로 인스턴스를 생성한다.
/// 사용 후 반드시 <see cref="DisposeAsync"/>를 호출하거나 <c>await using</c>으로 감싼다.
/// </summary>
public sealed partial class IksungReader : IAsyncDisposable
{
    private readonly IIksungChannel _channel;
    private bool _disposed;
    private bool _autoReadActive;
    // System features (see IksungReader.System.cs)
    private string? _portOrAddress;

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
        _channel.PacketReceived    += OnChannelPacketReceived;
        _channel.ConnectionChanged += OnChannelConnectionChanged;
        _channel.RawDataReceived   += OnChannelRawDataReceived;
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
        var reader = new IksungReader(channel, ChannelType.Serial);
        reader._portOrAddress = $"{portName} @ {baudRate} bps";
        return reader;
    }

    /// <summary>
    /// 시리얼 포트로 리더기에 연결하고, 연결 옵션을 적용한다.
    /// </summary>
    /// <param name="portName">포트 이름</param>
    /// <param name="baudRate">통신 속도</param>
    /// <param name="options">연결 옵션 (<see cref="IksungReaderOptions"/>)</param>
    /// <param name="ct">취소 토큰</param>
    public static async Task<IksungReader> ConnectSerialAsync(
        string portName,
        int baudRate,
        IksungReaderOptions options,
        CancellationToken ct = default)
    {
        var channel = new SerialIksungChannel(portName, baudRate);
        await channel.ConnectAsync(ct).ConfigureAwait(false);
        var reader = new IksungReader(channel, ChannelType.Serial);
        reader._portOrAddress = $"{portName} @ {baudRate} bps";
        reader.ApplyOptions(options);
        return reader;
    }

    /// <summary>
    /// PC/SC (CCID) 리더로 연결한다.
    /// <see cref="IksungPcscDiscovery.GetAvailableReaders"/> 로 리더 이름을 얻을 수 있다.
    /// </summary>
    /// <param name="readerName">PC/SC 리더 이름 (예: "iksung IS-3500Z 0")</param>
    /// <param name="options">연결 옵션 (선택)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>연결된 <see cref="IksungReader"/> 인스턴스</returns>
    public static async Task<IksungReader> ConnectPcscAsync(
        string readerName,
        IksungReaderOptions? options = null,
        CancellationToken ct = default)
    {
        var channel = new PcscIksungChannel(readerName);
        bool ok = await channel.ConnectAsync(ct).ConfigureAwait(false);
        if (!ok) throw new Exceptions.ChannelDisconnectedException(
            $"PC/SC 리더에 연결할 수 없습니다: {readerName}");
        var reader = new IksungReader(channel, ChannelType.Pcsc);
        reader._portOrAddress = readerName;
        if (options != null) reader.ApplyOptions(options);
        return reader;
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
        _intentionalDisconnect = true;
        _channel.PacketReceived    -= OnChannelPacketReceived;
        _channel.ConnectionChanged -= OnChannelConnectionChanged;
        _channel.RawDataReceived   -= OnChannelRawDataReceived;
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
        if (_options.LogRawPackets)
            RawPacketReceived?.Invoke(this, new RawPacketEventArgs(packet, isTransmit: true));
        return await _channel.SendAndReceiveAsync(packet, timeoutMs, ct).ConfigureAwait(false);
    }

    private void OnChannelPacketReceived(object? sender, IksungPacket pkt)
    {
        if (!_autoReadActive) return;
        if (pkt.Cmd1 != Constants.MAJOR_AUTO) return;

        var (cardType, uid) = MapAutoReadPacket(pkt);
        TagDetected?.Invoke(this, new TagDetectedEventArgs(cardType, uid, pkt.Cmd1, pkt.Cmd2, pkt.Data));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IksungReader));
    }
}
