/*
 * Sample 07 – Mifare DESFire EV1/EV2 (.NET Framework 4.x)
 * ==========================================================
 * DESFire 카드의 전체 워크플로를 보여줍니다:
 *   1. 카드 활성화 + UID 읽기
 *   2. 인증 키 저장 (AES-128 기본 키: 16 × 0x00)
 *   3. Master Key 인증 (Key #0, AES)
 *   4. 여유 메모리 조회
 *   5. Application ID 목록 조회
 *   6. 기존 Application 선택 (또는 Root 선택)
 *   7. 파일 ID 목록 조회
 *   8. Standard File 읽기 / 쓰기
 *
 * ⚠️  주의: 실제 카드 데이터를 변경합니다. 테스트용 카드를 사용하세요.
 *           Master Key가 기본값(16 × 0x00)이 아닌 경우 인증이 실패합니다.
 *
 * 사용법:
 *   Desfire.Net4x.exe COM3
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace Desfire.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";

            // DESFire AES-128 기본 공장 키 (16 바이트 all-zero)
            byte[] defaultAesKey = new byte[16];        // 0x00 * 16
            const byte CryptoTypeAes = 0x00;            // DESFIRE_ENC_AES_128
            const byte KeyVersion    = 0x00;
            const byte MasterKeyNo   = 0x00;

            Console.WriteLine($"[IKSUNG] Connecting to {portName}...");

            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
                Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}");
                Console.WriteLine("[IKSUNG] Place a Mifare DESFire EV1/EV2 card. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // ── 1. 활성화 ──
                            byte[] uid = await reader.ActivateDesfireAsync(1000, cts.Token);
                            Console.WriteLine($"\n{"─",50}");
                            Console.WriteLine($"DESFire detected  UID : {Hex(uid)}");

                            // ── 2. 인증 키 Flash 저장 ──
                            await reader.DesfireKeySaveAsync(0, KeyVersion, CryptoTypeAes, defaultAesKey, 1000, cts.Token);
                            Console.WriteLine("  Key saved to reader flash (index=0, AES-128, all-zero)");

                            // ── 3. Master Key 인증 ──
                            bool authOk = false;
                            try
                            {
                                await reader.DesfireAuthenticateAesAsync(MasterKeyNo, 2000, cts.Token);
                                Console.WriteLine($"  AES Auth (Key #{MasterKeyNo})          : OK");
                                authOk = true;
                            }
                            catch (IksungProtocolException)
                            {
                                Console.WriteLine($"  AES Auth (Key #{MasterKeyNo})          : FAIL (key mismatch?)");
                            }

                            if (!authOk) goto NextCard;

                            // ── 4. 여유 메모리 ──
                            byte[] freeMemBytes = await reader.DesfireGetFreeMemoryAsync(1000, cts.Token);
                            int freeMem = (freeMemBytes.Length >= 3)
                                ? freeMemBytes[0] | (freeMemBytes[1] << 8) | (freeMemBytes[2] << 16)
                                : 0;
                            Console.WriteLine($"  Free memory                   : {freeMem} bytes");

                            // ── 5. Application ID 목록 ──
                            byte[] appIds = await reader.DesfireGetApplicationIdsAsync(1000, cts.Token);
                            int appCount  = appIds.Length / 3;
                            Console.WriteLine($"  Application count             : {appCount}");
                            for (int i = 0; i < appCount; i++)
                            {
                                byte[] aid = appIds.Skip(i * 3).Take(3).ToArray();
                                Console.WriteLine($"    AppID[{i}]: {HexNoSep(aid)}");
                            }

                            // ── 6. Application 선택 ──
                            if (appCount > 0)
                            {
                                byte[] firstAid = appIds.Take(3).ToArray();
                                Console.WriteLine($"\n  Selecting App {HexNoSep(firstAid)}...");
                                await reader.DesfireSelectApplicationAsync(firstAid, 1000, cts.Token);

                                // App 내부 인증 (Key #0)
                                await reader.DesfireKeySaveAsync(0, KeyVersion, CryptoTypeAes, defaultAesKey, 1000, cts.Token);
                                try
                                {
                                    await reader.DesfireAuthenticateAesAsync(0, 2000, cts.Token);
                                    Console.WriteLine("  App Key #0 Auth               : OK");
                                }
                                catch (IksungProtocolException)
                                {
                                    Console.WriteLine("  App Key #0 Auth               : FAIL (different key)");
                                }

                                // ── 7. File ID 목록 ──
                                try
                                {
                                    byte[] fileIds = await reader.DesfireGetFileIdsAsync(1000, cts.Token);
                                    Console.WriteLine($"  File count                    : {fileIds.Length}");

                                    // ── 8. 첫 번째 파일 읽기 ──
                                    if (fileIds.Length > 0)
                                    {
                                        byte fileNo = fileIds[0];
                                        Console.WriteLine($"\n  Reading file #{fileNo}:");
                                        try
                                        {
                                            byte[] fileSettings = await reader.DesfireGetFileSettingsAsync(fileNo, 1000, cts.Token);
                                            Console.WriteLine($"    File settings : {Hex(fileSettings)}");

                                            byte[] fileData = await reader.DesfireReadDataFileAsync(fileNo, 0, 0, 0, 2000, cts.Token);
                                            Console.WriteLine($"    File data     : {Hex(fileData)}");
                                            Console.WriteLine($"    ASCII         : [{ToAscii(fileData)}]");

                                            // 첫 16바이트를 현재 타임스탬프로 덮어씀
                                            if (fileData.Length >= 16)
                                            {
                                                byte[] writeData = new byte[fileData.Length];
                                                fileData.CopyTo(writeData, 0);
                                                string ts = DateTime.Now.ToString("yyyyMMdd HHmmss");
                                                System.Text.Encoding.ASCII.GetBytes(ts).CopyTo(writeData, 0);

                                                await reader.DesfireWriteDataFileAsync(fileNo, writeData, 0, 0, 2000, cts.Token);
                                                await reader.DesfireCommitTransactionAsync(1000, cts.Token);
                                                Console.WriteLine($"    Written       : {Hex(writeData)}  (timestamp)");
                                            }
                                        }
                                        catch (IksungProtocolException ex) { Console.WriteLine($"    File op error : {ex.Message}"); }
                                    }
                                }
                                catch (IksungProtocolException) { Console.WriteLine("  File list      : not accessible"); }
                            }
                            else
                            {
                                Console.WriteLine("\n  (No applications found on card — card may be brand new)");
                                Console.WriteLine("  Select Root application:");
                                await reader.DesfireSelectRootAsync(1000, cts.Token);
                                Console.WriteLine("  Root selected OK.");
                            }

                            NextCard:;
                        }
                        catch (IksungProtocolException ex) { Console.WriteLine($"  Protocol error: {ex.Message}"); }
                        catch (IksungTimeoutException)     { /* no card */ }
                        catch (OperationCanceledException) { break; }

                        try { await Task.Delay(800, cts.Token); }
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

        static string Hex(byte[] b)      => BitConverter.ToString(b).Replace("-", " ");
        static string HexNoSep(byte[] b) => BitConverter.ToString(b).Replace("-", "");

        static string ToAscii(byte[] b)
            => new string(b.Select(x => x >= 0x20 && x < 0x7F ? (char)x : '.').ToArray());
    }
}
