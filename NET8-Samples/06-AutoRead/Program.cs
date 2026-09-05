/*
 * Sample 06 – AutoRead (Event-Driven Card Detection)
 * ====================================================
 * 리더기의 AutoRead 폴링 모드를 사용합니다.
 * 카드를 감지하면 리더기가 자동으로 패킷을 전송하고,
 * SDK가 TagDetected 이벤트로 알려줍니다.
 *
 * AutoRead는 여러 카드 타입(ISO14443A/B, ISO15693, Felica, LF)을
 * 동시에 폴링하므로 폴링 주기 설정이 필요 없습니다.
 *
 * 사용법:
 *   (인수 없이 실행)                리더를 자동으로 찾습니다 — 포트를 몰라도 됩니다
 *   dotnet run -- COM3                      (Serial — Windows)
 *   dotnet run -- /dev/ttyUSB0              (Serial — Linux)
 *   dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 */

using Iksung.Reader;

// ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
string firstArg = args.Length > 0 ? args[0] : "(자동 탐색)";

// ── 연결: 인수 없으면 자동 탐색 / "COMx" = Serial / "pcsc[:이름]" = PC/SC ──
IksungReader? reader = await SampleConnect.OpenAsync(args);
if (reader == null) return;
await using var _ = reader;
// ── 연결 확인 ─────────────────────────────────────────────────
Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
if (!await reader.PingAsync(timeoutMs: 500))
{
    Console.WriteLine($"[ERROR] 리더기가 응답하지 않습니다. (연결: {firstArg})");
    Console.WriteLine("  - 리더기 전원을 확인하세요.");
    Console.WriteLine("  - 케이블 연결을 확인하세요.");
    return;
}
Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");

int count = 0;

// ── 이벤트 핸들러 등록 (백그라운드 스레드에서 호출됨) ──
reader.TagDetected += (_, e) =>
{
    count++;
    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
    ConsoleColor color = e.CardType switch
    {
        CardType.Iso14443a      => ConsoleColor.Cyan,
        CardType.Iso14443b      => ConsoleColor.Green,
        CardType.MifareClassic  => ConsoleColor.Yellow,
        CardType.MifareUltralight => ConsoleColor.Magenta,
        CardType.Iso15693       => ConsoleColor.Blue,
        CardType.Felica         => ConsoleColor.Red,
        CardType.Lf125Khz       => ConsoleColor.DarkYellow,
        _                       => ConsoleColor.White,
    };

    Console.ForegroundColor = color;
    Console.WriteLine($"{timestamp}  #{count:D4}  [{e.CardType,-18}]  UID: {e.UidHex}");
    Console.ResetColor();
};

// ── AutoRead 시작 ──
await reader.StartAutoReadAsync();
Console.WriteLine("[IKSUNG] AutoRead started. Place cards on the reader. Press Ctrl+C to stop.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try { await Task.Delay(Timeout.Infinite, cts.Token); }
catch (OperationCanceledException) { }

// ── AutoRead 중지 ──
Console.WriteLine("\n[IKSUNG] Stopping AutoRead...");
await reader.StopAutoReadAsync();
Console.WriteLine($"[IKSUNG] Done. Total tags detected: {count}");
