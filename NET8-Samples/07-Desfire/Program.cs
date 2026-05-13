/*
 * Sample 07 – Mifare DESFire EV1/EV2
 * =====================================
 * DESFire 카드의 전체 워크플로를 보여줍니다:
 *   1. 카드 활성화 + UID 읽기
 *   2. 인증 키 저장 (AES-128 기본 키: 16 × 0x00)
 *   3. Master Key 인증 (Key #0, AES)
 *   4. 여유 메모리 조회
 *   5. Application ID 목록 조회
 *   6. 기존 Application 선택 (또는 Root 선택)
 *   7. 파일 ID 목록 조회
 *   8. Standard File 읽기 / 쓰기
 *
 * ⚠️  주의: 실제 카드 데이터를 변경합니다. 테스트용 카드를 사용하세요.
 *           Master Key가 기본값(16 × 0x00)이 아닌 경우 인증이 실패합니다.
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

// DESFire AES-128 기본 공장 키 (16 바이트 all-zero)
byte[] defaultAesKey = new byte[16];            // 0x00 * 16
const byte CryptoTypeAes = 0x00;                // DESFIRE_ENC_AES_128
const byte KeyVersion    = 0x00;
const byte MasterKeyNo   = 0x00;

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
Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}");
Console.WriteLine("[IKSUNG] Place a Mifare DESFire EV1/EV2 card. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // ── 1. 활성화 ──
        byte[] uid = await reader.ActivateDesfireAsync(1000, cts.Token);
        Console.WriteLine($"\n{'─',50}");
        Console.WriteLine($"DESFire detected  UID : {Hex(uid)}");

        // ── 2. 인증 키 Flash 저장 ──
        await reader.DesfireKeySaveAsync(0, KeyVersion, CryptoTypeAes, defaultAesKey, 1000, cts.Token);
        Console.WriteLine("  Key saved to reader flash (index=0, AES-128, all-zero)");

        // ── 3. Master Key 인증 ──
        try
        {
            await reader.DesfireAuthenticateAesAsync(MasterKeyNo, 2000, cts.Token);
            Console.WriteLine($"  AES Auth (Key #{MasterKeyNo})          : OK");
        }
        catch (IksungProtocolException)
        {
            Console.WriteLine($"  AES Auth (Key #{MasterKeyNo})          : FAIL (key mismatch?)");
            goto NextCard;
        }

        // ── 4. 여유 메모리 ──
        byte[] freeMemBytes = await reader.DesfireGetFreeMemoryAsync(1000, cts.Token);
        int freeMem = (freeMemBytes.Length >= 3)
            ? freeMemBytes[0] | (freeMemBytes[1] << 8) | (freeMemBytes[2] << 16)
            : 0;
        Console.WriteLine($"  Free memory                   : {freeMem} bytes");

        // ── 5. Application ID 목록 ──
        byte[] appIds = await reader.DesfireGetApplicationIdsAsync(1000, cts.Token);
        int appCount  = appIds.Length / 3;
        Console.WriteLine($"  Application count             : {appCount}");
        for (int i = 0; i < appCount; i++)
        {
            byte[] aid = appIds[(i * 3)..((i + 1) * 3)];
            Console.WriteLine($"    AppID[{i}]: {HexNoSep(aid)}");
        }

        // ── 6. Application 선택 ──
        if (appCount > 0)
        {
            byte[] firstAid = appIds[0..3];
            Console.WriteLine($"\n  Selecting App {HexNoSep(firstAid)}...");
            await reader.DesfireSelectApplicationAsync(firstAid, 1000, cts.Token);

            // App 내부 인증 (Key #0)
            await reader.DesfireKeySaveAsync(0, KeyVersion, CryptoTypeAes, defaultAesKey, 1000, cts.Token);
            try
            {
                await reader.DesfireAuthenticateAesAsync(0, 2000, cts.Token);
                Console.WriteLine("  App Key #0 Auth               : OK");
            }
            catch (IksungProtocolException)
            {
                Console.WriteLine("  App Key #0 Auth               : FAIL (different key)");
            }

            // ── 7. File ID 목록 ──
            try
            {
                byte[] fileIds = await reader.DesfireGetFileIdsAsync(1000, cts.Token);
                Console.WriteLine($"  File count                    : {fileIds.Length}");

                // ── 8. 첫 번째 파일 읽기 ──
                if (fileIds.Length > 0)
                {
                    byte fileNo = fileIds[0];
                    Console.WriteLine($"\n  Reading file #{fileNo}:");
                    try
                    {
                        byte[] fileSettings = await reader.DesfireGetFileSettingsAsync(fileNo, 1000, cts.Token);
                        Console.WriteLine($"    File settings : {Hex(fileSettings)}");

                        byte[] fileData = await reader.DesfireReadDataFileAsync(fileNo, 0, 0, 0, 2000, cts.Token);
                        Console.WriteLine($"    File data     : {Hex(fileData)}");
                        Console.WriteLine($"    ASCII         : [{ToAscii(fileData)}]");

                        // 첫 16바이트를 현재 타임스탬프로 덮어씀
                        if (fileData.Length >= 16)
                        {
                            byte[] writeData = new byte[fileData.Length];
                            fileData.CopyTo(writeData, 0);
                            string ts = DateTime.Now.ToString("yyyyMMdd HHmmss");
                            System.Text.Encoding.ASCII.GetBytes(ts).CopyTo(writeData, 0);

                            await reader.DesfireWriteDataFileAsync(fileNo, writeData, 0, 0, 2000, cts.Token);
                            await reader.DesfireCommitTransactionAsync(1000, cts.Token);
                            Console.WriteLine($"    Written       : {Hex(writeData)}  (timestamp)");
                        }
                    }
                    catch (IksungProtocolException ex) { Console.WriteLine($"    File op error : {ex.Message}"); }
                }
            }
            catch (IksungProtocolException) { Console.WriteLine("  File list      : not accessible"); }
        }
        else
        {
            Console.WriteLine("\n  (No applications found on card — card may be brand new)");
            Console.WriteLine("  Select Root application:");
            await reader.DesfireSelectRootAsync(1000, cts.Token);
            Console.WriteLine("  Root selected OK.");
        }

        NextCard:;
    }
    catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
    catch (IksungTimeoutException)     { /* no card */ }
    catch (OperationCanceledException) { break; }

    try { await Task.Delay(800, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static string Hex(byte[] b)      => BitConverter.ToString(b).Replace("-", " ");
static string HexNoSep(byte[] b) => BitConverter.ToString(b).Replace("-", "");
static string ToAscii(byte[] b)
    => new string(b.Select(x => x is >= 0x20 and < 0x7F ? (char)x : '.').ToArray());
