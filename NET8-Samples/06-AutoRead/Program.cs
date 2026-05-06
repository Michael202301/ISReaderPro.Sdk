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
 *   dotnet run -- COM3
 */

using Iksung.Reader;

string portName = args.Length > 0 ? args[0] : "COM3";

Console.WriteLine($"[IKSUNG] Connecting to {portName}...");
await using var reader = await IksungReader.ConnectSerialAsync(portName);
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
