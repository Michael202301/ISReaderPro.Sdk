/*
 * Sample net4x-01 – Hello World (.NET Framework 4.7.2)
 * ======================================================
 * .NET Framework 4.x에서 Iksung.Reader SDK를 사용하는 기본 예제입니다.
 *
 * .NET 8 예제와의 차이점:
 *  1. 클래스·메서드 선언 필요 — top-level statements 미지원
 *  2. using 지시문 명시 필요 — implicit usings 미지원
 *  3. new byte[] { } — 컬렉션 표현식 [...] 미지원 (단, LangVersion=latest 로 사용 가능)
 *  4. await using 미지원 → try/finally + DisposeAsync().GetAwaiter().GetResult()
 *  5. 인덱스 연산자 ^n → Length - n 으로 대체 (또는 LangVersion=latest 유지)
 *
 * 빌드:
 *   dotnet build
 * 실행:
 *   dotnet run -- COM3
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace HelloWorld.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine("[IKSUNG] Connecting to " + portName + "...");

            // .NET 4.x: await using 대신 try/finally 사용
            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
                string fw = await reader.ReadVersionAsync();
                Console.WriteLine("[IKSUNG] Firmware : " + fw);
                Console.WriteLine("[IKSUNG] Connected : " + reader.IsConnected);
                Console.WriteLine("[IKSUNG] Channel  : " + reader.ConnectedVia);

                Console.WriteLine("\n[IKSUNG] Place a card on the reader. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        cts.Cancel();
                    };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
                            Console.WriteLine(
                                DateTime.Now.ToString("HH:mm:ss") +
                                "  UID: " + BitConverter.ToString(uid).Replace("-", ""));
                            await Task.Delay(500, cts.Token);
                        }
                        catch (IksungTimeoutException)
                        {
                            // 카드 없음 — 계속 대기
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                // .NET 4.x: await using 대신 ValueTask를 동기적으로 완료
                await reader.DisposeAsync();
            }

            Console.WriteLine("\n[IKSUNG] Done.");
        }
    }
}
