using Iksung.Reader.Internals.Channels;

namespace Iksung.Reader;

/// <summary>
/// PC/SC (CCID) 리더 검색 유틸리티.
/// Windows 의 winscard.dll (Smart Card Service) 를 통해 연결된 CCID 리더 목록을 반환한다.
/// </summary>
public static class IksungPcscDiscovery
{
    /// <summary>
    /// 현재 시스템에 연결된 PC/SC 리더 이름 목록을 반환한다.
    /// Smart Card Service(SCardSvr) 가 중지된 경우 빈 목록을 반환한다.
    /// </summary>
    /// <returns>리더 이름 목록 (예: ["iksung IS-3500Z 0", "ACS ACR39U ICC Reader 0"])</returns>
    public static IReadOnlyList<string> GetAvailableReaders()
        => PcscIksungChannel.ListReaders();

    /// <summary>
    /// 백그라운드에서 리더 목록을 모니터링하다가 변경 시 이벤트를 발사한다.
    /// <see cref="StartMonitor"/> 를 호출해야 모니터링이 시작된다.
    /// </summary>
    public static event EventHandler<IReadOnlyList<string>>? ReaderListChanged;

    private static CancellationTokenSource? _monitorCts;
    private static IReadOnlyList<string>    _lastReaders = Array.Empty<string>();

    /// <summary>
    /// 리더 목록 모니터링을 시작한다. 이미 실행 중이면 무시한다.
    /// </summary>
    /// <param name="pollIntervalMs">폴링 간격 (ms, 기본값: 1000)</param>
    public static void StartMonitor(int pollIntervalMs = 1000)
    {
        if (_monitorCts != null) return;
        _monitorCts = new CancellationTokenSource();
        var ct = _monitorCts.Token;

        Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var current = GetAvailableReaders();
                    if (!ListsEqual(current, _lastReaders))
                    {
                        _lastReaders = current;
                        ReaderListChanged?.Invoke(null, current);
                    }
                    await Task.Delay(pollIntervalMs, ct).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) { }
        }, ct);
    }

    /// <summary>리더 목록 모니터링을 중지한다.</summary>
    public static void StopMonitor()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _monitorCts = null;
    }

    private static bool ListsEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
        return true;
    }
}
