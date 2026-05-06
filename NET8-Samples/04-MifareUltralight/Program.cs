/*
 * Sample 04 – Mifare Ultralight / NTag
 * =====================================
 * Mifare Ultralight C 또는 NTag 카드를 활성화하고
 * 페이지를 읽고 쓰는 예제입니다.
 *
 * 페이지 구조 (MF ULC):
 *   0–1: Serial number / check byte
 *   2:   Lock bytes (read only)
 *   3:   OTP
 *   4~:  User data pages (4 bytes each)
 *
 * 사용법:
 *   dotnet run -- COM3
 *   dotnet run -- COM3 115200 5   (대상 페이지 5)
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

string portName  = args.Length > 0 ? args[0] : "COM3";
byte   targetPage = args.Length > 2 && byte.TryParse(args[2], out byte pg) ? pg : (byte)5;
const int TotalPages = 16;

Console.WriteLine($"[IKSUNG] Connecting to {portName}...");
await using var reader = await IksungReader.ConnectSerialAsync(portName);
Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");
Console.WriteLine($"[IKSUNG] Target page: {targetPage}");
Console.WriteLine("[IKSUNG] Place a Mifare Ultralight / NTag card. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // ── 1. 활성화 ──
        byte[] uid = await reader.ActivateMifareUltralightAsync(1000, cts.Token);
        Console.WriteLine($"\nUltralight / NTag detected  UID: {Hex(uid)}");

        // ── 2. 전체 페이지 덤프 ──
        Console.WriteLine($"  Page dump (pages 0–{TotalPages - 1}):");
        for (byte p = 0; p < TotalPages; p++)
        {
            try
            {
                byte[] pageData = await reader.MifareUltralightReadPageAsync(p, 500, cts.Token);
                Console.WriteLine($"    Page {p:D2}: {Hex(pageData)}  [{ToAscii(pageData)}]");
            }
            catch (IksungProtocolException) { Console.WriteLine($"    Page {p:D2}: (read error)"); }
        }

        // ── 3. 사용자 페이지 쓰기 (페이지 targetPage 이상만) ──
        if (targetPage >= 4)
        {
            byte[] writeData = new byte[4];
            uint ts = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            writeData[0] = (byte)(ts >> 24);
            writeData[1] = (byte)(ts >> 16);
            writeData[2] = (byte)(ts >> 8);
            writeData[3] = (byte)ts;

            Console.Write($"\n  Write page {targetPage}: {Hex(writeData)} (Unix timestamp)  ... ");
            await reader.MifareUltralightWritePageAsync(targetPage, writeData, 1000, cts.Token);
            Console.WriteLine("OK");

            byte[] verify = await reader.MifareUltralightReadPageAsync(targetPage, 500, cts.Token);
            bool match = verify.AsSpan().SequenceEqual(writeData);
            Console.WriteLine($"  Verify page {targetPage}: {(match ? "PASS ✓" : "FAIL ✗")}  {Hex(verify)}");
        }
    }
    catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
    catch (IksungTimeoutException)     { /* no card */ }
    catch (OperationCanceledException) { break; }

    try { await Task.Delay(600, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static string Hex(byte[] b) => BitConverter.ToString(b).Replace("-", " ");
static string ToAscii(byte[] b)
    => new string(b.Select(x => x is >= 0x20 and < 0x7F ? (char)x : '.').ToArray());
