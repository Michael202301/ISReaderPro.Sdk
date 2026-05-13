namespace Iksung.Reader;

/// <summary>
/// 리더기의 시스템 정보를 담는 불변 구조체.
/// <see cref="IksungReader.GetReaderInfoAsync"/> 로 조회한다.
/// </summary>
public readonly struct ReaderInfo
{
    /// <summary>펌웨어 버전 문자열 (예: "IS-3500K_V1.8").</summary>
    public string FirmwareVersion { get; }

    /// <summary>연결에 사용된 채널 종류.</summary>
    public ChannelType Channel { get; }

    /// <summary>현재 연결 여부.</summary>
    public bool IsConnected { get; }

    /// <summary>
    /// 포트 이름 또는 원격 주소.
    /// Serial: "COM3 @ 115200 bps" / TCP: "192.168.1.10:1470"
    /// </summary>
    public string? PortOrAddress { get; }

    /// <summary>구조체를 생성한다.</summary>
    public ReaderInfo(string firmwareVersion, ChannelType channel, bool isConnected, string? portOrAddress)
    {
        FirmwareVersion = firmwareVersion;
        Channel         = channel;
        IsConnected     = isConnected;
        PortOrAddress   = portOrAddress;
    }

    /// <inheritdoc/>
    public override string ToString()
        => $"[{Channel}] {PortOrAddress} | FW: {FirmwareVersion} | {(IsConnected ? "Connected" : "Disconnected")}";
}
