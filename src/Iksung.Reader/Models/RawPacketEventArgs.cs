namespace Iksung.Reader;

/// <summary>
/// <see cref="IksungReader.RawPacketReceived"/> 이벤트 인자.
/// <see cref="IksungReaderOptions.LogRawPackets"/>가 <c>true</c>일 때만 발생한다.
/// </summary>
public sealed class RawPacketEventArgs : EventArgs
{
    /// <summary>on-wire raw 바이트 배열.</summary>
    public byte[] Data { get; }

    /// <summary><c>true</c> = 송신(TX), <c>false</c> = 수신(RX).</summary>
    public bool IsTransmit { get; }

    /// <summary>패킷이 캡처된 시각.</summary>
    public DateTime Timestamp { get; }

    /// <summary>인스턴스를 생성한다.</summary>
    /// <param name="data">raw 바이트</param>
    /// <param name="isTransmit"><c>true</c>=TX, <c>false</c>=RX</param>
    public RawPacketEventArgs(byte[] data, bool isTransmit)
    {
        Data       = data;
        IsTransmit = isTransmit;
        Timestamp  = DateTime.Now;
    }
}
