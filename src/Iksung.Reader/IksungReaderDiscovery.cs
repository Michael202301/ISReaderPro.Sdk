using System.IO.Ports;

namespace Iksung.Reader;

/// <summary>
/// Iksung 리더기가 연결된 포트를 탐색하는 유틸리티 클래스.
/// </summary>
public static class IksungReaderDiscovery
{
    // VERSION REQUEST 패킷 (7 bytes, 데이터 없음)
    // STX(01) CMD1(00) CMD2(10) LEN_H(00) LEN_L(00) CHKSUM(10) ETX(03)
    // CHKSUM = XOR(CMD1, CMD2, LEN_H, LEN_L) = 0x00 ^ 0x10 ^ 0x00 ^ 0x00 = 0x10
    private static readonly byte[] VersionRequestPacket =
        [0x01, 0x00, 0x10, 0x00, 0x00, 0x10, 0x03];

    /// <summary>
    /// 시스템에 등록된 모든 시리얼 포트 이름을 반환한다.
    /// </summary>
    public static IEnumerable<string> GetAllSerialPorts()
        => SerialPort.GetPortNames();

    /// <summary>
    /// 각 시리얼 포트에 Version 패킷을 전송하여 Iksung 리더기가 응답하는 포트 목록을 반환한다.
    /// </summary>
    /// <param name="baudRate">테스트에 사용할 통신 속도. 기본값: 115200</param>
    /// <param name="pingTimeoutMs">포트당 응답 대기 시간 (ms). 기본값: 400</param>
    /// <param name="progressCallback">각 포트 스캔 완료 시 호출되는 진행 콜백. (포트 이름, Iksung 여부)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>Iksung 리더기가 응답한 포트 이름 목록</returns>
    public static async Task<IReadOnlyList<string>> ScanIksungPortsAsync(
        int baudRate      = 115200,
        int pingTimeoutMs = 400,
        IProgress<(string portName, bool isIksung)>? progressCallback = null,
        CancellationToken ct = default)
    {
        var ports   = SerialPort.GetPortNames();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Parallel.ForEachAsync is .NET 6+; use SemaphoreSlim for net472 compatibility.
        using var sem = new SemaphoreSlim(4);
        var tasks = ports.Select(async port =>
        {
            await sem.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                bool isIksung = await PingPortAsync(port, baudRate, pingTimeoutMs, ct)
                                      .ConfigureAwait(false);
                progressCallback?.Report((port, isIksung));
                if (isIksung) results.Add(port);
            }
            finally { sem.Release(); }
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.ToArray();
    }

    /// <summary>
    /// 단일 시리얼 포트에 Version 패킷을 전송하여 Iksung 프로토콜 응답이 오는지 확인한다.
    /// 이미 다른 프로세스가 사용 중인 포트는 <c>false</c>를 반환한다.
    /// </summary>
    /// <param name="portName">확인할 포트 이름</param>
    /// <param name="baudRate">통신 속도. 기본값: 115200</param>
    /// <param name="timeoutMs">응답 대기 시간 (ms). 기본값: 400</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>Iksung 리더기가 응답하면 <c>true</c></returns>
    public static async Task<bool> PingPortAsync(
        string portName,
        int baudRate  = 115200,
        int timeoutMs = 400,
        CancellationToken ct = default)
    {
        SerialPort? port = null;
        try
        {
            port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout  = timeoutMs,
                WriteTimeout = 500,
                DtrEnable    = true,
            };

            await Task.Run(() => port.Open(), ct).ConfigureAwait(false);
            await Task.Run(() => port.Write(VersionRequestPacket, 0, VersionRequestPacket.Length), ct)
                      .ConfigureAwait(false);

            var buf = new byte[32];
            return await Task.Run(() =>
            {
                try
                {
                    int b = port.ReadByte();   // 첫 바이트: STX(0x01) 이면 Iksung 응답
                    return b == 0x01;
                }
                catch (TimeoutException) { return false; }
                catch (Exception)        { return false; }
            }, ct).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException) { return false; }   // 포트 사용 중
        catch (System.IO.IOException)       { return false; }
        catch (OperationCanceledException)  { return false; }
        catch (Exception)                   { return false; }
        finally
        {
            try { port?.Close();   } catch { }
            try { port?.Dispose(); } catch { }
        }
    }
}
