/*
 * Hello World — 가장 먼저 실행해 볼 예제 (.NET Framework 4.x)
 * ==========================================================
 * 리더에 연결해 펌웨어 버전과 고유 ID를 출력합니다.
 *
 * 사용법:
 *   HelloWorld.Console.Net4x.exe                   리더 자동 탐색 (권장)
 *   HelloWorld.Console.Net4x.exe COM40             Serial 포트 지정
 *   HelloWorld.Console.Net4x.exe COM40 115200      보드레이트까지 지정
 *   HelloWorld.Console.Net4x.exe pcsc              PC/SC(USB CCID) 리더
 *   HelloWorld.Console.Net4x.exe pcsc:iksung       PC/SC 리더 이름 일부로 지정
 *
 * 플랫폼: net472 는 x86 필수 (SerialPort 가 x64 에서 액세스 위반으로 죽습니다).
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
            // ── 연결: 인수가 없으면 리더를 자동으로 찾습니다 ──────────────
            IksungReader? reader = await SampleConnect.OpenAsync(args);
            if (reader == null) return;

            try
            {
                // ── 연결 확인 ─────────────────────────────────────────────
                System.Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
                bool alive = await reader.PingAsync(timeoutMs: 500);
                if (!alive)
                {
                    System.Console.WriteLine("[ERROR] 리더기가 응답하지 않습니다.");
                    System.Console.WriteLine("  - 리더기 전원과 케이블 연결을 확인하세요.");
                    System.Console.WriteLine("  - 다른 프로그램(ISReaderPro 등)이 리더를 잡고 있지 않은지 확인하세요.");
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
