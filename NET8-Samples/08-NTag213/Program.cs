/*
 * Sample 08 – NXP NTag 213 / 215 / 216
 * ========================================
 * NTag 칩의 고급 기능을 보여줍니다:
 *   1. 활성화 + UID
 *   2. Get Version → 칩 타입 자동 감지 (NTag213/215/216)
 *   3. ECC 서명 읽기 (칩 진위 확인)
 *   4. 카운터 읽기
 *   5. Fast Read (전체 사용자 영역 한 번에 읽기)
 *   6. 페이지 쓰기 (사용자 영역)
 *   7. 패스워드 보호 설정 예제 (주석 처리됨, 신중하게 사용할 것)
 *
 * NTag213 메모리 레이아웃 (45 pages, 180 bytes total):
 *   Page 0–1 : Serial number (UID)
 *   Page 2   : Internal / lock bytes
 *   Page 3   : Capability Container (CC)
 *   Page 4–39: User data (144 bytes)
 *   Page 40  : CFG 0 (AUTH0, ACCESS)
 *   Page 41  : CFG 1 (PWD, PACK)
 *   Page 42–44: (reserved)
 *
 * 사용법:
 *   dotnet run -- COM3
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

string portName = args.Length > 0 ? args[0] : "COM3";

Console.WriteLine($"[IKSUNG] Connecting to {portName}...");
await using var reader = await IksungReader.ConnectSerialAsync(portName);
Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}");
Console.WriteLine("[IKSUNG] Place an NTag 213/215/216 card. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // ── 1. 활성화 ──
        byte[] uid = await reader.ActivateMifareUltralightAsync(1000, cts.Token);
        Console.WriteLine($"\n{'─',55}");
        Console.WriteLine($"NTag detected  UID : {Hex(uid)}");

        // ── 2. Get Version → 칩 타입 감지 ──
        string chipType = "Unknown";
        byte userStart  = 4;
        byte userEnd    = 39;
        try
        {
            byte[] ver = await reader.NTagGetVersionAsync(1000, cts.Token);
            chipType  = IksungReader.ParseNTagType(ver);
            Console.WriteLine($"  Chip type          : {chipType}");
            Console.WriteLine($"  Version raw        : {Hex(ver)}");
            // NTag215 user area: pages 4-129, NTag216: pages 4-225
            userEnd = chipType switch { "NTag215" => 129, "NTag216" => 225, _ => 39 };
        }
        catch (IksungProtocolException)
        {
            Console.WriteLine("  Chip type          : (Get Version not supported)");
        }

        // ── 3. ECC 서명 ──
        try
        {
            byte[] sig = await reader.NTagReadSignatureAsync(2000, cts.Token);
            Console.WriteLine($"  ECC Signature      : {Hex(sig[..8])}... ({sig.Length} bytes)");
        }
        catch (IksungProtocolException)
        {
            Console.WriteLine("  ECC Signature      : (not available)");
        }

        // ── 4. 카운터 (NTag213/215 기준 counter 0) ──
        try
        {
            byte[] counter = await reader.NTagReadCounterAsync(0, 1000, cts.Token);
            int counterVal = counter.Length >= 3
                ? counter[0] | (counter[1] << 8) | (counter[2] << 16)
                : 0;
            Console.WriteLine($"  Counter #0         : {counterVal}");
        }
        catch (IksungProtocolException)
        {
            Console.WriteLine("  Counter #0         : (not supported)");
        }

        // ── 5. Fast Read (사용자 데이터 전체) ──
        Console.WriteLine($"\n  Fast Read pages {userStart}–{Math.Min(userEnd, (byte)(userStart + 15))} (first 16 pages of user area):");
        byte fastEnd = (byte)Math.Min(userEnd, userStart + 15);
        try
        {
            byte[] fastData = await reader.NTagFastReadAsync(userStart, fastEnd, 2000, cts.Token);
            int pageCount = fastData.Length / 4;
            for (int i = 0; i < pageCount; i++)
            {
                byte[] page = fastData[(i * 4)..((i + 1) * 4)];
                Console.WriteLine($"    Page {userStart + i:D3}: {Hex(page)}  [{ToAscii(page)}]");
            }
        }
        catch (IksungProtocolException)
        {
            // Fallback: single-page reads
            Console.WriteLine("  (Fast Read failed, reading page by page...)");
            for (byte p = userStart; p <= Math.Min(fastEnd, (byte)(userStart + 7)); p++)
            {
                try
                {
                    byte[] page = await reader.MifareUltralightReadPageAsync(p, 500, cts.Token);
                    Console.WriteLine($"    Page {p:D3}: {Hex(page)}  [{ToAscii(page)}]");
                }
                catch (IksungProtocolException) { Console.WriteLine($"    Page {p:D3}: (error)"); }
            }
        }

        // ── 6. 페이지 쓰기 (사용자 영역 첫 번째 페이지 = page 4) ──
        {
            byte writePage = userStart;
            byte[] writeData = new byte[4];
            uint ts = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            writeData[0] = (byte)(ts >> 24);
            writeData[1] = (byte)(ts >> 16);
            writeData[2] = (byte)(ts >> 8);
            writeData[3] = (byte)ts;

            Console.Write($"\n  Write page {writePage}: {Hex(writeData)} (Unix timestamp BE)  ... ");
            await reader.MifareUltralightWritePageAsync(writePage, writeData, 1000, cts.Token);
            Console.WriteLine("OK");

            byte[] verify = await reader.MifareUltralightReadPageAsync(writePage, 500, cts.Token);
            Console.WriteLine($"  Verify page {writePage}: {(verify.AsSpan().SequenceEqual(writeData) ? "PASS ✓" : "FAIL ✗")}  {Hex(verify)}");
        }

        /*
         * ── 7. 패스워드 보호 (주석 해제 시 카드가 잠길 수 있음) ──
         *
         * byte[] pwd  = [0x01, 0x23, 0x45, 0x67];  // 4-byte password
         * byte[] pack = [0x89, 0xAB];               // 2-byte PACK
         * await reader.NTagWriteAuth0Async(4, 1000, cts.Token);    // auth from page 4
         * await reader.NTagWriteAccessAsync(0x80, 1000, cts.Token);// PROT=1: R+W protected
         * await reader.NTagChangePasswordAsync(pwd, pack, 1000, cts.Token);
         * Console.WriteLine("  Password protection set.");
         *
         * // 인증 해제:
         * await reader.NTagPasswordAuthAsync(pwd, 1000, cts.Token);
         */
    }
    catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
    catch (IksungTimeoutException)     { /* no card */ }
    catch (OperationCanceledException) { break; }

    try { await Task.Delay(700, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static string Hex(byte[] b)
    => BitConverter.ToString(b).Replace("-", " ");
static string ToAscii(byte[] b)
    => new string(b.Select(x => x is >= 0x20 and < 0x7F ? (char)x : '.').ToArray());
