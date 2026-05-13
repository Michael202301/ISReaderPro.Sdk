/*
 * Sample 03 – Mifare Classic 1K / 4K (.NET Framework 4.x)
 * =========================================================
 * Mifare Classic 카드를 활성화하고, 기본 키로 인증 후 블록을 읽고 씁니다.
 * 주의: 실제 카드 데이터를 변경하므로 테스트용 카드를 사용하세요.
 *
 * 기본 키: FF FF FF FF FF FF (Mifare Classic 초기 공장 키)
 *
 * 사용법:
 *   MifareClassic.Net4x.exe COM3                      (Serial — Windows)
 *   MifareClassic.Net4x.exe COM3 115200 4             (Serial, 블록 4 대상)
 *   MifareClassic.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   MifareClassic.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
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

namespace MifareClassic.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg    = args.Length > 0 ? args[0] : "COM3";
            byte   targetBlock = args.Length > 2 && byte.TryParse(args[2], out byte bl) ? bl : (byte)4;

            // Mifare Classic 공장 기본 키 A
            byte[] defaultKeyA = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

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
                Console.WriteLine($"[IKSUNG] Target block: {targetBlock} (sector {targetBlock / 4})");
                Console.WriteLine("[IKSUNG] Place a Mifare Classic 1K/4K card. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
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
                            await reader.MifareAuthenticateAsync(targetBlock, MifareKeyType.KeyA, defaultKeyA, 1000, cts.Token);
                            byte[] verify = await reader.MifareReadBlockAsync(targetBlock, 1000, cts.Token);
                            bool match = verify.SequenceEqual(writeData);
                            Console.WriteLine($"  Verify       : {(match ? "PASS ✓" : "FAIL ✗")}  {Hex(verify)}");
                        }
                        catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
                        catch (IksungTimeoutException)     { /* no card */ }
                        catch (OperationCanceledException) { break; }

                        try { await Task.Delay(500, cts.Token); }
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
