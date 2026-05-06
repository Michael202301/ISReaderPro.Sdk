/*
 * Sample 02 – ISO 14443-A / ISO-DEP (T=CL) (.NET Framework 4.x)
 * ================================================================
 * ISO 14443-A 카드를 활성화하고 APDU 교환 예제를 보여줍니다.
 * 스마트카드(NFC Forum Type 4, ISO-DEP)가 있으면 SELECT PPSE APDU도 전송합니다.
 *
 * 사용법:
 *   Iso14443a.Net4x.exe COM3
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace Iso14443a.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine($"[IKSUNG] Connecting to {portName}...");

            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
                Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");
                Console.WriteLine("[IKSUNG] Place an ISO 14443-A card on the reader. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // ── 1단계: Layer-3 활성화 (UID만 읽음) ──
                            byte[] uid3 = await reader.ActivateIso14443aAsync(1000, cts.Token);
                            Console.WriteLine($"\nCard detected (Layer-3)  UID: {Hex(uid3)}");

                            // ── 2단계: Layer-4 (ISO-DEP) 활성화 시도 ──
                            try
                            {
                                byte[] uid4 = await reader.ActivateIso14443_4aAsync(1000, cts.Token);
                                Console.WriteLine($"  ISO-DEP activated       UID+ATS: {Hex(uid4)}");

                                // ── 3단계: GET CHALLENGE (ISO 7816-4) ──
                                // 카드에서 8바이트 난수를 요청합니다.
                                // CLA=00 INS=84 P1=00 P2=00 Le=08
                                byte[] getChallenge = new byte[]
                                {
                                    0x00, 0x84, 0x00, 0x00, 0x08     // GET CHALLENGE, Le=8
                                };

                                byte[] resp = await reader.ExchangeApduAsync(getChallenge, 3000, cts.Token);
                                Console.WriteLine($"  GET CHALLENGE response: {Hex(resp)}");

                                // SW1 SW2 상태 코드 해석
                                if (resp.Length >= 2)
                                {
                                    byte sw1 = resp[resp.Length - 2];
                                    byte sw2 = resp[resp.Length - 1];
                                    Console.WriteLine($"  Status word: {sw1:X2} {sw2:X2}  ({InterpretSw(sw1, sw2)})");
                                    if (sw1 == 0x90 && sw2 == 0x00 && resp.Length >= 10)
                                    {
                                        byte[] random = new byte[resp.Length - 2];
                                        Array.Copy(resp, 0, random, 0, random.Length);
                                        Console.WriteLine($"  Random bytes (8):       {Hex(random)}");
                                    }
                                }
                            }
                            catch (IksungProtocolException)
                            {
                                Console.WriteLine("  (Layer-4 not supported — card is Layer-3 only)");
                            }

                            // ── Halt ──
                            await reader.HaltIso14443aAsync(500, cts.Token);
                            Console.WriteLine("  Card halted.");
                        }
                        catch (IksungProtocolException)    { /* no card */ }
                        catch (IksungTimeoutException)     { /* no card */ }
                        catch (OperationCanceledException) { break; }

                        try { await Task.Delay(300, cts.Token); }
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

        static string Hex(byte[] b) => BitConverter.ToString(b).Replace("-", " ");

        static string InterpretSw(byte sw1, byte sw2)
        {
            if (sw1 == 0x90 && sw2 == 0x00) return "SUCCESS";
            if (sw1 == 0x6A && sw2 == 0x82) return "FILE NOT FOUND";
            if (sw1 == 0x69 && sw2 == 0x85) return "CONDITIONS NOT SATISFIED";
            return "see ISO 7816-4";
        }
    }
}
