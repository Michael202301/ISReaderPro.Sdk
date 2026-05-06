/*
 * Sample net4x-02 – Mifare Classic (.NET Framework 4.7.2)
 * =========================================================
 * Mifare Classic 카드 인증 → 블록 읽기 → 블록 쓰기 → 검증 예제
 *
 * 사용법:
 *   dotnet run -- COM3
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace MifareClassic.Net4x
{
    internal class Program
    {
        // Mifare 기본 키 (공장 출하값)
        private static readonly byte[] DefaultKey = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        static async Task Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine("[IKSUNG] Connecting to " + portName + "...");

            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
                Console.WriteLine("[IKSUNG] Firmware : " + await reader.ReadVersionAsync());
                Console.WriteLine("[IKSUNG] Mifare Classic 카드를 올려 주세요. (Ctrl+C 종료)\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // ── 1. 카드 활성화 ──
                            byte[] atr = await reader.ActivateMifareAsync(1000, cts.Token);
                            Console.WriteLine("카드 활성화: " + BitConverter.ToString(atr).Replace("-", " "));

                            const byte BlockNo = 4; // 섹터 1의 첫 번째 데이터 블록

                            // ── 2. Key A 인증 ──
                            await reader.MifareAuthenticateAsync(
                                BlockNo, MifareKeyType.KeyA, DefaultKey, 1000, cts.Token);
                            Console.WriteLine("Key A 인증 성공");

                            // ── 3. 블록 읽기 ──
                            byte[] before = await reader.MifareReadBlockAsync(BlockNo, 1000, cts.Token);
                            Console.WriteLine("읽기 전: " + BitConverter.ToString(before).Replace("-", " "));

                            // ── 4. 현재 시각을 16바이트 ASCII로 쓰기 ──
                            byte[] writeData = new byte[16];
                            string ts = DateTime.Now.ToString("yyyyMMdd HHmmss");
                            byte[] tsBytes = Encoding.ASCII.GetBytes(ts);
                            Array.Copy(tsBytes, 0, writeData, 0,
                                       Math.Min(tsBytes.Length, writeData.Length));

                            await reader.MifareWriteBlockAsync(BlockNo, writeData, 1000, cts.Token);
                            Console.WriteLine("쓰기 완료: " + ts);

                            // ── 5. 검증 읽기 ──
                            byte[] after = await reader.MifareReadBlockAsync(BlockNo, 1000, cts.Token);
                            Console.WriteLine("읽기 후: " + BitConverter.ToString(after).Replace("-", " "));

                            Console.WriteLine("완료.\n");
                            await Task.Delay(3000, cts.Token);
                        }
                        catch (IksungTimeoutException)
                        {
                            await Task.Delay(300, cts.Token);
                        }
                        catch (IksungProtocolException ex)
                        {
                            Console.WriteLine("프로토콜 오류: " + ex.Message + "\n");
                            await Task.Delay(1000, cts.Token);
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
                await reader.DisposeAsync();
            }

            Console.WriteLine("[IKSUNG] Done.");
        }
    }
}
