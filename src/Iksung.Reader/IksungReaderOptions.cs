namespace Iksung.Reader;

/// <summary>
/// <see cref="IksungReader"/> 동작을 제어하는 설정 옵션.
/// </summary>
/// <remarks>
/// <see cref="IksungReader.ConnectSerialAsync(string, int, IksungReaderOptions, CancellationToken)"/> 에 전달하거나,
/// 연결 후 <see cref="IksungReader.ApplyOptions"/> 로 적용한다.
/// 런타임 적용 시 <see cref="AutoReconnect"/>, <see cref="LogRawPackets"/>는 즉시 반영된다.
/// </remarks>
public sealed class IksungReaderOptions
{
    /// <summary>
    /// 비정상 연결 끊김(케이블 뽑힘 등) 시 자동 재연결 여부.
    /// <see cref="IksungReader.DisconnectAsync"/>를 명시적으로 호출하면 재연결하지 않는다.
    /// 기본값: <c>false</c>
    /// </summary>
    public bool AutoReconnect { get; set; } = false;

    /// <summary>
    /// 자동 재연결 시도 간격 (밀리초). 기본값: 2000
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 2000;

    /// <summary>
    /// <see cref="IksungReader.PingAsync"/> 등에서 사용하는 기본 응답 대기 시간 (밀리초). 기본값: 1000
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 1000;

    /// <summary>
    /// <c>true</c>이면 <see cref="IksungReader.RawPacketReceived"/> 이벤트를 통해
    /// 송수신 raw 바이트를 노출한다. 기본값: <c>false</c>
    /// </summary>
    /// <remarks>
    /// 활성화하면 패킷마다 byte[] 복사본이 생성되므로 고속 폴링 환경에서는 신중히 사용한다.
    /// </remarks>
    public bool LogRawPackets { get; set; } = false;
}
