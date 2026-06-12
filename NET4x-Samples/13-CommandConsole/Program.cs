/*
 * Sample 13 – Interactive Command Console (.NET Framework 4.x)
 * ==============================================================
 * IS-3500K에 RAW 명령을 대화형으로 보내고 응답을 확인하는 디버그 콘솔.
 * 프로토콜을 직접 탐색하거나 새 명령을 테스트할 때 유용합니다.
 *
 * 명령 형식:
 *   <CMD1_hex> <CMD2_hex> [DATA_hex...]   예: 00 10            (버전 읽기)
 *                                             01 10            (ISO14443A 활성화)
 *                                             02 20 60 00 01   (Mifare 인증)
 *
 * 내장 단축 명령:
 *   version   — 펌웨어 버전 읽기
 *   uid       — UID 읽기
 *   rfon      — RF On
 *   rfoff     — RF Off
 *   help      — 도움말
 *   quit / q  — 종료
 *
 * 사용법:
 *   CommandConsole.Net4x.exe COM3                      (Serial — Windows)
 *   CommandConsole.Net4x.exe COM3 115200               (Serial, 보드레이트 지정)
 *   CommandConsole.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   CommandConsole.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace CommandConsole.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║    IKSUNG Reader — Interactive Command Console        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.WriteLine($"\nConnecting to {firstArg}...");

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
                string fw = await reader.ReadVersionAsync();
                Console.WriteLine($"Connected. Firmware: {fw}\n");

                PrintHelp();
                Console.WriteLine();

                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("cmd> ");
                    Console.ResetColor();

                    string? lineNullable = Console.ReadLine();
                    string line = lineNullable == null ? "" : lineNullable.Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    // ── 내장 단축 명령 ──
                    string lower = line.ToLower();
                    if (lower == "quit" || lower == "q" || lower == "exit") break;
                    if (lower == "help") { PrintHelp(); continue; }

                    if (lower == "version")
                    {
                        string ver = await reader.ReadVersionAsync();
                        PrintOk($"Firmware version: {ver}");
                        continue;
                    }
                    if (lower == "uid")
                    {
                        await RunSafeAsync(async () =>
                        {
                            byte[] uid = await reader.ReadUniqueIdAsync();
                            PrintOk($"Unique ID: {Hex(uid)}");
                        });
                        continue;
                    }
                    if (lower == "rfon")
                    {
                        await RunSafeAsync(async () => { await reader.RfOnAsync(); PrintOk("RF ON"); });
                        continue;
                    }
                    if (lower == "rfoff")
                    {
                        await RunSafeAsync(async () => { await reader.RfOffAsync(); PrintOk("RF OFF"); });
                        continue;
                    }

                    // ── RAW 명령 파싱: "CMD1 CMD2 [DATA...]" ──
                    string[] tokens = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length < 2)
                    {
                        PrintError("Need at least CMD1 and CMD2. Type 'help' for usage.");
                        continue;
                    }

                    if (!TryParseHex(tokens[0], out byte cmd1) || !TryParseHex(tokens[1], out byte cmd2))
                    {
                        PrintError("CMD1/CMD2 must be hex bytes (e.g., 00 10).");
                        continue;
                    }

                    byte[] data = new byte[0];
                    if (tokens.Length > 2)
                    {
                        var dataList = new List<byte>();
                        bool parseOk = true;
                        for (int i = 2; i < tokens.Length; i++)
                        {
                            if (!TryParseHex(tokens[i], out byte b))
                            {
                                PrintError($"Invalid hex byte: '{tokens[i]}'");
                                parseOk = false;
                                break;
                            }
                            dataList.Add(b);
                        }
                        if (!parseOk) continue;
                        data = dataList.ToArray();
                    }

                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  TX: CMD1=0x{cmd1:X2} CMD2=0x{cmd2:X2}" +
                                      (data.Length > 0 ? $" DATA={Hex(data)}" : " (no data)"));
                    Console.ResetColor();

                    await RunSafeAsync(async () =>
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        byte[] response = await reader.SendRawCommandAsync(cmd1, cmd2, data, 3000);
                        sw.Stop();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"  RX ({sw.ElapsedMilliseconds}ms): ");
                        Console.ResetColor();

                        if (response.Length == 0)
                        {
                            Console.WriteLine("(empty response)");
                        }
                        else
                        {
                            // 첫 줄: hex dump
                            Console.WriteLine(Hex(response));

                            // 두 번째 줄: ASCII 가시문자
                            string ascii = new string(response.Select(b => b >= 0x20 && b < 0x7F ? (char)b : '.').ToArray());
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"  ASCII: {ascii}");
                            Console.ResetColor();

                            // 세 번째 줄: 십진수
                            string dec = string.Join(" ", response.Select(b => $"{b,3}"));
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"  DEC:   {dec}");
                            Console.ResetColor();
                        }
                    });
                }

                Console.WriteLine("\n[IKSUNG] Goodbye.");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  version             — read firmware version");
            Console.WriteLine("  uid                 — read reader unique ID");
            Console.WriteLine("  rfon                — RF antenna ON");
            Console.WriteLine("  rfoff               — RF antenna OFF");
            Console.WriteLine("  help                — show this help");
            Console.WriteLine("  quit / q            — exit");
            Console.WriteLine();
            Console.WriteLine("Raw protocol:");
            Console.WriteLine("  <CMD1> <CMD2> [DATA...]    (all hex bytes, space-separated)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  00 10               — Read firmware version");
            Console.WriteLine("  00 14               — Read unique ID");
            Console.WriteLine("  00 1A               — RF ON");
            Console.WriteLine("  00 1B               — RF OFF");
            Console.WriteLine("  01 10               — ISO14443A activate");
            Console.WriteLine("  03 10               — ISO15693 activate");
            Console.WriteLine("  12 10               — BLE read name");
            Console.WriteLine("  22 20               — Relay read all inputs");
            Console.WriteLine("  22 30               — Relay read all outputs");
        }

        static void PrintOk(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  OK: {msg}");
            Console.ResetColor();
        }

        static void PrintError(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ERR: {msg}");
            Console.ResetColor();
        }

        static string Hex(byte[] b)
            => b.Length == 0 ? "(empty)" : BitConverter.ToString(b).Replace("-", " ");

        static bool TryParseHex(string s, out byte value)
        {
            string cleaned = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s.Substring(2) : s;
            return byte.TryParse(cleaned, NumberStyles.HexNumber, null, out value);
        }

        static async Task RunSafeAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (IksungProtocolException ex)
            {
                PrintError($"Protocol: {ex.Message}");
            }
            catch (IksungTimeoutException ex)
            {
                PrintError($"Timeout: {ex.Message}");
            }
            catch (Exception ex)
            {
                PrintError($"Unexpected: {ex.Message}");
            }
        }
    }
}
