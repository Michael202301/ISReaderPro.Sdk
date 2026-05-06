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
 *   AutoRead.Net4x.exe COM3
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
            string portName = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine($"[IKSUNG] Connecting to {portName}...");

            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
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
