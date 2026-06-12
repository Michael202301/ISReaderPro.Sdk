/*
 * Sample 04 – Mifare Ultralight / NTag (.NET Framework 4.x)
 * ===========================================================
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
 *   MifareUltralight.Net4x.exe COM3                      (Serial — Windows)
 *   MifareUltralight.Net4x.exe COM3 115200 5             (Serial, 대상 페이지 5)
 *   MifareUltralight.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   MifareUltralight.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 *
 * 플랫폼:
 * ┌──────────────────────────────────────────────────────────────────────────┐
 * │  ⚠  x86 필수 — .NET Framework 4.7.2 SerialPort 64비트 버그            │
 * │                                                                          │
 * │  .NET Framework 4.7.2 의 SerialPort 는 64비트 환경에서 치명적 버그    │
 * │  (접근 위반 0xC0000005) 가 있습니다. 반드시 x86 으로 빌드·실행        │
 * │  하십시오. x64 / Any CPU 로 실행하면 크래시가 발생합니다.              │
 * │                                                                          │
 * │  Visual Studio: 구성 관리자 → 플랫폼 → x86                             │
 * │  CLI: dotnet build -p:Platform=x86 / dotnet run -p:Platform=x86       │
 * └──────────────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace MifareUltralight.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg   = args.Length > 0 ? args[0] : "COM3";
            byte   targetPage = args.Length > 2 && byte.TryParse(args[2], out byte pg) ? pg : (byte)5;
            const int TotalPages = 16;

            IksungReader reader;
            if (firstArg.StartsWith("pcsc", StringComparison.OrdinalIgnoreCase))
            {
                string? readerName = firstArg.Contains(":")
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
                    Console.WriteLine("[IKSUNG] PC/SC 리더 자동 선택: " + readerName);
                }
                else
                {
                    Console.WriteLine("[IKSUNG] PC/SC 연결: " + readerName);
                }
                reader = await IksungReader.ConnectPcscAsync(readerName);
            }
            else
            {
                Console.WriteLine("[IKSUNG] Serial 연결: " + firstArg);
                reader = await IksungReader.ConnectSerialAsync(firstArg);
            }

            try
            {
                // ── 연결 확인 ─────────────────────────────────────────────────
                Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
                bool alive = await reader.PingAsync(timeoutMs: 500);
                if (!alive)
                {
                    Console.WriteLine("[ERROR] 리더기가 응답하지 않습니다. (연결: " + firstArg + ")");
                    Console.WriteLine("  - 리더기 전원을 확인하세요.");
                    Console.WriteLine("  - 케이블 연결을 확인하세요.");
                    return;
                }
                Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");
                Console.WriteLine($"[IKSUNG] Target page: {targetPage}");
                Console.WriteLine("[IKSUNG] Place a Mifare Ultralight / NTag card. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
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
                                bool match = verify.SequenceEqual(writeData);
                                Console.WriteLine($"  Verify page {targetPage}: {(match ? "PASS ✓" : "FAIL ✗")}  {Hex(verify)}");
                            }
                        }
                        catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
                        catch (IksungTimeoutException)     { /* no card */ }
                        catch (OperationCanceledException) { break; }

                        try { await Task.Delay(600, cts.Token); }
                        catch (OperationCanceledException) { break; }
                    }
                }

                Console.WriteLine("\n[IKSUNG] Done.");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        static string Hex(byte[] b) => BitConverter.ToString(b).Replace("-", " ");

        static string ToAscii(byte[] b)
            => new string(b.Select(x => x >= 0x20 && x < 0x7F ? (char)x : '.').ToArray());
    }
}
