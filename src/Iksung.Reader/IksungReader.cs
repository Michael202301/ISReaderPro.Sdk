namespace Iksung.Reader;

/// <summary>
/// IKSUNG NFC/RFID 리더기 SDK 의 진입점.
/// </summary>
public sealed class IksungReader : IAsyncDisposable
{
    /// <summary>현재 채널 연결 여부.</summary>
    public bool IsConnected => false;

    /// <summary>연결된 채널 종류.</summary>
    public ChannelType ConnectedVia { get; private init; }

    private IksungReader(ChannelType type) => ConnectedVia = type;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
