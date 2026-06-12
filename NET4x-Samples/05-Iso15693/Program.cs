/*
 * Sample 05 – ISO 15693 (Vicinity Cards / iCODE) (.NET Framework 4.x)
 * ======================================================================
 * ISO 15693 (13.56 MHz vicinity) 카드를 활성화하고
 * 여러 블록을 읽는 예제입니다.
 * 대표 카드: NXP ICODE SLI/SLI-S, Texas Instruments Tag-it HF-I
 *
 * 사용법:
 *   Iso15693.Net4x.exe COM3                      (Serial — Windows)
 *   Iso15693.Net4x.exe COM3 115200 0 8           (Serial, 블록 0부터 8블록 읽기)
 *   Iso15693.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   Iso15693.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
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

namespace Iso15693.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg   = args.Length > 0 ? args[0] : "COM3";
            byte   firstBlock = args.Length > 2 && byte.TryParse(args[2], out byte fb) ? fb : (byte)0;
            byte   blockCount = args.Length > 3 && byte.TryParse(args[3], out byte bc) ? bc : (byte)8;

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
                Console.WriteLine($"[IKSUNG] Reading blocks {firstBlock}–{firstBlock + blockCount - 1}");
                Console.WriteLine("[IKSUNG] Place an ISO 15693 card. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // ── 1. 활성화 ──
                            byte[] uid = await reader.ActivateIso15693Async(1000, cts.Token);
                            // ISO 15693 UID는 little-endian이므로 표시 시 역순
                            Console.WriteLine($"\nISO 15693 detected  UID: {HexRev(uid)}  (LSB first: {Hex(uid)})");

                            // ── 2. 다중 블록 읽기 (한 번에) ──
                            try
                            {
                                byte[] multi = await reader.Iso15693ReadMultipleBlocksAsync(firstBlock, blockCount, 3000, cts.Token);
                                int stride = multi.Length / blockCount;
                                Console.WriteLine($"  Multiple-block read ({blockCount} blocks, {stride} bytes each):");
                                for (int i = 0; i < blockCount; i++)
                                {
                                    int offset = i * stride;
                                    byte[] block = multi.Skip(offset).Take(stride).ToArray();
                                    Console.WriteLine($"    Block {firstBlock + i:D3}: {Hex(block)}  [{ToAscii(block)}]");
                                }
                            }
                            catch (IksungProtocolException)
                            {
                                // Fallback: 단일 블록씩 읽기
                                Console.WriteLine("  (Multiple-block not supported, reading one by one...)");
                                for (byte b = firstBlock; b < firstBlock + blockCount; b++)
                                {
                                    try
                                    {
                                        byte[] block = await reader.Iso15693ReadBlockAsync(b, 500, cts.Token);
                                        Console.WriteLine($"    Block {b:D3}: {Hex(block)}  [{ToAscii(block)}]");
                                    }
                                    catch (IksungProtocolException) { Console.WriteLine($"    Block {b:D3}: (error)"); }
                                }
                            }
                        }
                        catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
                        catch (IksungTimeoutException)     { /* no card */ }
                        catch (OperationCanceledException) { break; }

                        try { await Task.Delay(700, cts.Token); }
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

        static string Hex(byte[] b)    => BitConverter.ToString(b).Replace("-", " ");
        static string HexRev(byte[] b) => Hex(b.Reverse().ToArray());

        static string ToAscii(byte[] b)
            => new string(b.Select(x => x >= 0x20 && x < 0x7F ? (char)x : '.').ToArray());
    }
}
