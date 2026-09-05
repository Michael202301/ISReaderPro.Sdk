// =============================================================================
// SampleConnect — 예제 공통 연결 도우미
// =============================================================================
// 모든 예제 폴더에 같은 사본이 들어 있습니다. 예제 폴더만 복사해도 그대로 동작합니다.
//
// 실행 인수 규칙
//   (인수 없음)             리더를 자동으로 찾습니다 (Serial 스캔 → PC/SC 순)
//   COM40 · /dev/ttyUSB0    Serial 연결 (두 번째 인수가 숫자면 보드레이트)
//   pcsc                    PC/SC 첫 번째 리더에 연결
//   pcsc:iksung             PC/SC 리더 이름 부분 일치 (따옴표 없이 잘려도 찾아냅니다)
//
// 연결에 실패하면 "무엇을 확인해야 하는지"를 출력하고 null 을 돌려줍니다.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Iksung.Reader;

internal static class SampleConnect
{
    /// <summary>인수에 맞춰 리더에 연결한다. 실패 시 원인을 출력하고 null 을 반환한다.</summary>
    public static async Task<IksungReader?> OpenAsync(string[] args)
    {
        string? target = (args != null && args.Length > 0) ? args[0] : null;

        int baud = 115200;
        if (args != null && args.Length > 1 && int.TryParse(args[1], out int parsed))
            baud = parsed;

        if (string.IsNullOrWhiteSpace(target))
            return await AutoAsync(baud).ConfigureAwait(false);

        if (target!.StartsWith("pcsc", StringComparison.OrdinalIgnoreCase))
            return await PcscAsync(target).ConfigureAwait(false);

        return await SerialAsync(target!, baud).ConfigureAwait(false);
    }

    // ── 자동 탐색 ────────────────────────────────────────────────────────────
    private static async Task<IksungReader?> AutoAsync(int baud)
    {
        Console.WriteLine("[IKSUNG] 리더를 찾는 중입니다...");

        IReadOnlyList<string> ports;
        try { ports = await IksungReaderDiscovery.ScanIksungPortsAsync(baud).ConfigureAwait(false); }
        catch { ports = new string[0]; }

        if (ports.Count > 0)
        {
            Console.WriteLine("[IKSUNG] Serial 리더 발견: " + ports[0]);
            return await SerialAsync(ports[0], baud).ConfigureAwait(false);
        }

        var readers = PcscReaders();
        if (readers.Count > 0)
        {
            Console.WriteLine("[IKSUNG] PC/SC 리더 발견: " + readers[0]);
            return await ConnectPcscAsync(readers[0]).ConfigureAwait(false);
        }

        Console.WriteLine("[ERROR] 리더를 찾지 못했습니다.");
        PrintSerialPorts();
        PrintPcscHint();
        return null;
    }

    // ── PC/SC ────────────────────────────────────────────────────────────────
    private static async Task<IksungReader?> PcscAsync(string target)
    {
        string? want = null;
        int colon = target.IndexOf(':');
        if (colon >= 0) want = target.Substring(colon + 1).Trim();

        var readers = PcscReaders();
        if (readers.Count == 0)
        {
            Console.WriteLine("[ERROR] PC/SC 리더를 찾을 수 없습니다.");
            PrintPcscHint();
            PrintSerialPorts();
            return null;
        }

        string chosen;
        if (string.IsNullOrEmpty(want))
        {
            chosen = readers[0];
            Console.WriteLine("[IKSUNG] PC/SC 리더 자동 선택: " + chosen);
        }
        else
        {
            var hits = readers
                .Where(r => r.IndexOf(want!, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            if (hits.Count == 0)
            {
                Console.WriteLine("[ERROR] \"" + want + "\" 와(과) 일치하는 PC/SC 리더가 없습니다.");
                PrintList("연결된 PC/SC 리더", readers);
                return null;
            }

            chosen = hits[0];
            if (hits.Count > 1)
                Console.WriteLine("[IKSUNG] " + hits.Count + "개가 일치해 첫 번째를 사용합니다.");
            Console.WriteLine("[IKSUNG] PC/SC 연결: " + chosen);
        }

        return await ConnectPcscAsync(chosen).ConfigureAwait(false);
    }

    private static async Task<IksungReader?> ConnectPcscAsync(string readerName)
    {
        try
        {
            return await IksungReader.ConnectPcscAsync(readerName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] PC/SC 연결 실패: " + ex.Message);
            Console.WriteLine("  - 리더에 카드를 올려두고 다시 시도해 보세요(카드가 있어야 연결되는 리더가 있습니다).");
            return null;
        }
    }

    // ── Serial ───────────────────────────────────────────────────────────────
    private static async Task<IksungReader?> SerialAsync(string port, int baud)
    {
        Console.WriteLine("[IKSUNG] Serial 연결: " + port + " @ " + baud + " bps");
        try
        {
            return await IksungReader.ConnectSerialAsync(port, baud).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] " + port + " 에 연결하지 못했습니다: " + ex.Message);
            PrintSerialPorts();
            Console.WriteLine("  힌트: 인수 없이 실행하면 리더가 붙은 포트를 자동으로 찾습니다.");
            return null;
        }
    }

    // ── 진단 출력 ────────────────────────────────────────────────────────────
    private static IReadOnlyList<string> PcscReaders()
    {
        try { return IksungPcscDiscovery.GetAvailableReaders(); }
        catch { return new string[0]; }
    }

    private static void PrintSerialPorts()
    {
        string[] ports;
        try { ports = IksungReaderDiscovery.GetAllSerialPorts().ToArray(); }
        catch { ports = new string[0]; }

        if (ports.Length == 0)
            Console.WriteLine("  이 PC에 시리얼 포트가 없습니다.");
        else
            Console.WriteLine("  이 PC의 시리얼 포트: " + string.Join(", ", ports));
    }

    private static void PrintPcscHint()
    {
        Console.WriteLine("  - PC/SC 는 리더가 CCID 모드일 때만 잡힙니다.");
        Console.WriteLine("    ISReaderPro 에서 인터페이스를 CCID 로 바꾼 뒤 USB 를 다시 연결하세요.");
        Console.WriteLine("    (가상 COM 모드면 시리얼로만 접속됩니다 — 위 포트 목록 참고)");
        Console.WriteLine("  - Windows 스마트카드 서비스(SCardSvr)가 실행 중인지 확인하세요.");
    }

    private static void PrintList(string title, IReadOnlyList<string> items)
    {
        Console.WriteLine("  " + title + ":");
        for (int i = 0; i < items.Count; i++)
            Console.WriteLine("    [" + i + "] " + items[i]);
    }
}
