/*
 * Sample net4x-03 – AutoRead 이벤트 모드 (.NET Framework 4.7.2)
 * ==============================================================
 * 리더기가 카드를 감지하면 TagDetected 이벤트로 알려줍니다.
 * WinForms/WPF 앱에서 UI 스레드 마샬링 패턴 포함.
 *
 * 사용법:
 *   dotnet run -- COM3
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
        static async Task Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine("[IKSUNG] Connecting to " + portName + "...");

            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
                Console.WriteLine("[IKSUNG] Firmware : " + await reader.ReadVersionAsync());

                // ── 이벤트 핸들러 등록 ──
                // 이벤트는 백그라운드 스레드에서 발생합니다.
                // WinForms: Control.Invoke() / WPF: Dispatcher.Invoke() 사용 필요
                EventHandler<TagDetectedEventArgs> handler = OnTagDetected;
                reader.TagDetected += handler;

                // ── AutoRead 시작 ──
                await reader.StartAutoReadAsync();
                Console.WriteLine("[IKSUNG] AutoRead 시작. 카드를 올려 주세요. (Enter 종료)\n");

                // Console 앱: Enter 대기
                await Task.Run(() => Console.ReadLine());

                // ── AutoRead 중지 ──
                await reader.StopAutoReadAsync();

                // ── 이벤트 핸들러 해제 (중요: GC 누수 방지) ──
                reader.TagDetected -= handler;

                Console.WriteLine("[IKSUNG] AutoRead 중지.");
            }
            finally
            {
                await reader.DisposeAsync();
            }

            Console.WriteLine("[IKSUNG] Done.");
        }

        // ── 카드 감지 이벤트 핸들러 ──
        // ※ 이 메서드는 백그라운드 스레드에서 호출됩니다.
        // WinForms에서 UI 업데이트 시:
        //   myLabel.Invoke(new Action(() => myLabel.Text = e.UidHex));
        // WPF에서 UI 업데이트 시:
        //   Application.Current.Dispatcher.Invoke(() => myLabel.Content = e.UidHex);
        private static void OnTagDetected(object? sender, TagDetectedEventArgs e)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss.fff");

            ConsoleColor color;
            switch (e.CardType)
            {
                case CardType.MifareClassic:    color = ConsoleColor.Cyan;       break;
                case CardType.MifareUltralight: color = ConsoleColor.Green;      break;
                case CardType.MifareDesfire:    color = ConsoleColor.Yellow;     break;
                case CardType.Iso15693:         color = ConsoleColor.Magenta;    break;
                case CardType.Lf125Khz:         color = ConsoleColor.DarkYellow; break;
                default:                        color = ConsoleColor.Gray;       break;
            }

            Console.ForegroundColor = color;
            Console.WriteLine(ts + "  [" + e.CardType + "]  UID: " + e.UidHex);
            Console.ResetColor();
        }
    }
}
