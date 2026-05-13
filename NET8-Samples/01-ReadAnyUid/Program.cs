/*
 * Sample 01 – Read Any UID
 * ========================
 * 리더기에 연결해 NFC/RFID 카드 UID를 계속 읽어서 출력합니다.
 * ISO 14443-A/B, ISO 15693, LF 125 kHz 순서로 시도합니다.
 *
 * 사용법:
 *   dotnet run -- COM3                      (Serial — Windows)
 *   dotnet run -- /dev/ttyUSB0              (Serial — Linux)
 *   dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 *
 * 플랫폼:
 * ┌──────────────────────────────────────────────────────────────────────────┐
 * │  프로젝트가 x86 으로 고정되어 있습니다                                  │
 * │                                                                          │
 * │  .NET 8 의 SerialPort 는 x64 / x86 모두 안정적입니다. 이 솔루션은      │
 * │  NET4x-Samples 와 동일한 x86 플랫폼 설정을 사용합니다.                 │
 * │  (x64 빌드에서도 정상 동작합니다)                                       │
 * │                                                                          │
 * │  Visual Studio: 구성 관리자 → 플랫폼 → x86                             │
 * │  CLI: dotnet build -p:Platform=x86 / dotnet run -r win-x86             │
 * └──────────────────────────────────────────────────────────────────────────┘
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

// ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
string firstArg = args.Length > 0 ? args[0] : "COM3";

IksungReader reader;
if (firstArg.StartsWith("pcsc", StringComparison.OrdinalIgnoreCase))
{
    string? readerName = firstArg.Contains(':')
        ? firstArg.Substring(firstArg.IndexOf(':') + 1).Trim()
        : null;
    if (readerName == null)
    {
        var readers = IksungPcscDiscovery.GetAvailableReaders();
        if (readers.Count == 0)
        {
            Console.WriteLine("[ERROR] PC/SC 리더를 찾을 수 없습니다.");
            Console.WriteLine("  - USB CCID 리더(IS-3500Z 등)가 연결되어 있는지 확인하세요.");
            Console.WriteLine("  - Windows Smart Card Service(SCardSvr)가 실행 중인지 확인하세요.");
            return;
        }
        readerName = readers[0];
        Console.WriteLine($"[IKSUNG] PC/SC 리더 자동 선택: {readerName}");
    }
    else
    {
        Console.WriteLine($"[IKSUNG] PC/SC 연결: {readerName}");
    }
    reader = await IksungReader.ConnectPcscAsync(readerName);
}
else
{
    int baudRate = args.Length > 1 && int.TryParse(args[1], out int b) ? b : 115200;
    Console.WriteLine($"[IKSUNG] Serial 연결: {firstArg} @ {baudRate} bps");
    reader = await IksungReader.ConnectSerialAsync(firstArg, baudRate);
}
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
Console.WriteLine("[IKSUNG] Place a card on the reader. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    string? uidHex = null;
    string? cardKind = null;

    // ── ISO 14443-A ──
    try
    {
        byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
        uidHex   = BitConverter.ToString(uid).Replace("-", "");
        cardKind = "ISO 14443-A";
    }
    catch (IksungProtocolException) { }   // 카드 없음
    catch (IksungTimeoutException)  { }

    // ── ISO 14443-B ──
    if (uidHex == null)
    {
        try
        {
            byte[] uid = await reader.ReadIso14443bUidAsync(500, cts.Token);
            uidHex   = BitConverter.ToString(uid).Replace("-", "");
            cardKind = "ISO 14443-B";
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── ISO 15693 ──
    if (uidHex == null)
    {
        try
        {
            byte[] uid = await reader.ReadIso15693UidAsync(500, cts.Token);
            uidHex   = BitConverter.ToString(uid).Replace("-", "");
            cardKind = "ISO 15693";
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── LF 125 kHz ──
    if (uidHex == null)
    {
        try
        {
            byte[] uid = await reader.ReadLf125KhzUidAsync(800, cts.Token);
            uidHex   = BitConverter.ToString(uid).Replace("-", "");
            cardKind = "LF 125 kHz";
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    if (uidHex != null)
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}  [{cardKind,-12}]  UID: {uidHex}");

    try { await Task.Delay(200, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");
