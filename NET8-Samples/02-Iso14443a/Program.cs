/*
 * Sample 02 – ISO 14443-A / ISO-DEP (T=CL)
 * ==========================================
 * ISO 14443-A 카드를 활성화하고 APDU 교환 예제를 보여줍니다.
 * 스마트카드(NFC Forum Type 4, ISO-DEP)가 있으면 SELECT PPSE APDU도 전송합니다.
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
    Console.WriteLine($"[IKSUNG] Serial 연결: {firstArg}");
    reader = await IksungReader.ConnectSerialAsync(firstArg);
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
Console.WriteLine("[IKSUNG] Place an ISO 14443-A card on the reader. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // ── 1단계: Layer-3 활성화 (UID만 읽음) ──
        byte[] uid3 = await reader.ActivateIso14443aAsync(1000, cts.Token);
        Console.WriteLine($"\nCard detected (Layer-3)  UID: {Hex(uid3)}");

        // ── 2단계: Layer-4 (ISO-DEP) 활성화 시도 ──
        try
        {
            byte[] uid4 = await reader.ActivateIso14443_4aAsync(1000, cts.Token);
            Console.WriteLine($"  ISO-DEP activated       UID+ATS: {Hex(uid4)}");

            // ── 3단계: GET CHALLENGE (ISO 7816-4) ──
            // 카드에서 8바이트 난수를 요청합니다.
            // CLA=00 INS=84 P1=00 P2=00 Le=08
            byte[] getChallenge = [0x00, 0x84, 0x00, 0x00, 0x08];   // GET CHALLENGE, Le=8

            byte[] resp = await reader.ExchangeApduAsync(getChallenge, 3000, cts.Token);
            Console.WriteLine($"  GET CHALLENGE response: {Hex(resp)}");

            // SW1 SW2 상태 코드 해석
            if (resp.Length >= 2)
            {
                byte sw1 = resp[^2], sw2 = resp[^1];
                Console.WriteLine($"  Status word: {sw1:X2} {sw2:X2}  ({InterpretSw(sw1, sw2)})");
                if (sw1 == 0x90 && sw2 == 0x00 && resp.Length >= 10)
                    Console.WriteLine($"  Random bytes (8):       {Hex(resp[..^2])}");

            }
        }
        catch (IksungProtocolException)
        {
            Console.WriteLine("  (Layer-4 not supported — card is Layer-3 only)");
        }

        // ── Halt ──
        await reader.HaltIso14443aAsync(500, cts.Token);
        Console.WriteLine("  Card halted.");
    }
    catch (IksungProtocolException)  { /* no card */ }
    catch (IksungTimeoutException)   { /* no card */ }
    catch (OperationCanceledException) { break; }

    try { await Task.Delay(300, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static string Hex(byte[] b) => BitConverter.ToString(b).Replace("-", " ");
static string InterpretSw(byte sw1, byte sw2) => (sw1, sw2) switch
{
    (0x90, 0x00) => "SUCCESS",
    (0x6A, 0x82) => "FILE NOT FOUND",
    (0x69, 0x85) => "CONDITIONS NOT SATISFIED",
    _ => "see ISO 7816-4"
};
