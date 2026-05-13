/*
 * Sample 06 – AutoRead (Event-Driven Card Detection) (.NET Framework 4.x)
 * =========================================================================
 * 리더기의 AutoRead 폴링 모드를 사용합니다.
 * 카드를 감지하면 리더기가 자동으로 패킷을 전송하고,
 * SDK가 TagDetected 이벤트로 알려줍니다.
 *
 * AutoRead는 여러 카드 타입(ISO14443A/B, ISO15693, Felica, LF)을
 * 동시에 폴링하므로 폴링 주기 설정이 필요 없습니다.
 *
 * 사용법:
 *   AutoRead.Net4x.exe COM3                      (Serial — Windows)
 *   AutoRead.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   AutoRead.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
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
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;

namespace AutoRead.Net4x
{
    internal class Program
    {
        static int s_count = 0;

        static async Task Main(string[] args)
        {
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

                // ── 이벤트 핸들러 등록 (백그라운드 스레드에서 호출됨) ──
                EventHandler<TagDetectedEventArgs> handler = OnTagDetected;
                reader.TagDetected += handler;

                // ── AutoRead 시작 ──
                await reader.StartAutoReadAsync();
                Console.WriteLine("[IKSUNG] AutoRead started. Place cards on the reader. Press Ctrl+C to stop.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    try { await Task.Delay(Timeout.Infinite, cts.Token); }
                    catch (OperationCanceledException) { }
                }

                // ── AutoRead 중지 ──
                Console.WriteLine("\n[IKSUNG] Stopping AutoRead...");
                await reader.StopAutoReadAsync();
                reader.TagDetected -= handler;
                Console.WriteLine($"[IKSUNG] Done. Total tags detected: {s_count}");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        static void OnTagDetected(object? sender, TagDetectedEventArgs e)
        {
            int count = System.Threading.Interlocked.Increment(ref s_count);
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

            ConsoleColor color;
            switch (e.CardType)
            {
                case CardType.Iso14443a:       color = ConsoleColor.Cyan;       break;
                case CardType.Iso14443b:       color = ConsoleColor.Green;      break;
                case CardType.MifareClassic:   color = ConsoleColor.Yellow;     break;
                case CardType.MifareUltralight: color = ConsoleColor.Magenta;   break;
                case CardType.Iso15693:        color = ConsoleColor.Blue;       break;
                case CardType.Felica:          color = ConsoleColor.Red;        break;
                case CardType.Lf125Khz:        color = ConsoleColor.DarkYellow; break;
                default:                       color = ConsoleColor.White;      break;
            }

            Console.ForegroundColor = color;
            Console.WriteLine($"{timestamp}  #{count:D4}  [{e.CardType,-18}]  UID: {e.UidHex}");
            Console.ResetColor();
        }
    }
}
