/*
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
using System.Threading.Tasks;
using Iksung.Reader;

namespace HelloWorld.Console.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                System.Console.WriteLine("Usage: HelloWorld.Console <portName> [baudRate]");
                System.Console.WriteLine("  Windows : HelloWorld.Console COM3 115200");
                System.Console.WriteLine("  Linux   : HelloWorld.Console /dev/ttyUSB0 115200");
                System.Console.WriteLine("  Mac     : HelloWorld.Console /dev/tty.usbserial-XXXX 115200");
                return;
            }

            string portName = args[0];
            int baudRate = args.Length >= 2 ? int.Parse(args[1]) : 115200;

            System.Console.WriteLine($"Connecting to {portName} @ {baudRate} bps ...");

            var reader = await IksungReader.ConnectSerialAsync(portName, baudRate);
            try
            {
                // ── 연결 확인 ─────────────────────────────────────────────────
                System.Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
                bool alive = await reader.PingAsync(timeoutMs: 500);
                if (!alive)
                {
                    System.Console.WriteLine("[ERROR] 리더기가 응답하지 않습니다. (포트: " + portName + ")");
                    System.Console.WriteLine("  - 리더기 전원을 확인하세요.");
                    System.Console.WriteLine("  - 케이블 연결을 확인하세요.");
                    return;
                }
                System.Console.WriteLine($"Connected via : {reader.ConnectedVia}");
                System.Console.WriteLine($"Firmware ver  : {await reader.ReadVersionAsync()}");
                System.Console.WriteLine($"Unique ID     : {BitConverter.ToString(await reader.ReadUniqueIdAsync())}");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }
    }
}
