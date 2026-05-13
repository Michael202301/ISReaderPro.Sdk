/*
 * Sample 03 – Mifare Classic 1K / 4K
 * ====================================
 * Mifare Classic 카드를 활성화하고, 기본 키로 인증 후 블록을 읽고 씁니다.
 * 주의: 실제 카드 데이터를 변경하므로 테스트용 카드를 사용하세요.
 *
 * 기본 키: FF FF FF FF FF FF (Mifare Classic 초기 공장 키)
 *
 * 사용법:
 *   dotnet run -- COM3                      (Serial — Windows)
 *   dotnet run -- /dev/ttyUSB0              (Serial — Linux)
 *   dotnet run -- COM3 115200 4             (Serial, 블록 4 대상)
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
byte   targetBlock = args.Length > 2 && byte.TryParse(args[2], out byte bl) ? bl : (byte)4;

// Mifare Classic 공장 기본 키 A
byte[] defaultKeyA = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

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
Console.WriteLine($"[IKSUNG] Target block: {targetBlock} (sector {targetBlock / 4})");
Console.WriteLine("[IKSUNG] Place a Mifare Classic 1K/4K card. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // ── 1. 활성화 ──
        byte[] uid = await reader.ActivateMifareAsync(1000, cts.Token);
        Console.WriteLine($"\nMifare Classic detected  UID: {Hex(uid)}");

        // ── 2. Key A 인증 ──
        await reader.MifareAuthenticateAsync(targetBlock, MifareKeyType.KeyA, defaultKeyA, 1000, cts.Token);
        Console.WriteLine($"  Auth Block {targetBlock:D3} Key A: OK");

        // ── 3. 블록 읽기 ──
        byte[] blockData = await reader.MifareReadBlockAsync(targetBlock, 1000, cts.Token);
        Console.WriteLine($"  Read  Block {targetBlock:D3}: {Hex(blockData)}");
        Console.WriteLine($"           ASCII: [{ToAscii(blockData)}]");

        // ── 4. 블록 쓰기 (테스트용: 현재 시간 기록) ──
        byte[] writeData = new byte[16];
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        System.Text.Encoding.ASCII.GetBytes(timestamp).CopyTo(writeData, 0);

        Console.Write($"  Write Block {targetBlock:D3}: {Hex(writeData)}  ... ");
        await reader.MifareWriteBlockAsync(targetBlock, writeData, 1000, cts.Token);
        Console.WriteLine("OK");

        // ── 5. 쓰기 검증 ──
        // 재인증 후 읽어서 확인
        await reader.MifareAuthenticateAsync(targetBlock, MifareKeyType.KeyA, defaultKeyA, 1000, cts.Token);
        byte[] verify = await reader.MifareReadBlockAsync(targetBlock, 1000, cts.Token);
        bool match = verify.AsSpan().SequenceEqual(writeData);
        Console.WriteLine($"  Verify       : {(match ? "PASS ✓" : "FAIL ✗")}  {Hex(verify)}");
    }
    catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
    catch (IksungTimeoutException)     { /* no card */ }
    catch (OperationCanceledException) { break; }

    try { await Task.Delay(500, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static string Hex(byte[] b) => BitConverter.ToString(b).Replace("-", " ");
static string ToAscii(byte[] b)
    => new string(b.Select(x => x is >= 0x20 and < 0x7F ? (char)x : '.').ToArray());
