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

                                // ── 3단계: SELECT PPSE (EMV 결제 환경 선택) ──
                                // 2PAY.SYS.DDF01 (Payment System Directory)
                                byte[] ppse = new byte[]
                                {
                                    0x00, 0xA4, 0x04, 0x00,          // SELECT by DF name
                                    0x0E,                              // Lc = 14
                                    0x32, 0x50, 0x41, 0x59, 0x2E,    // 2PAY.
                                    0x53, 0x59, 0x53, 0x2E,           // SYS.
                                    0x44, 0x44, 0x46, 0x30, 0x31,    // DDF01
                                    0x00                              // Le
                                };

                                byte[] resp = await reader.ExchangeApduAsync(ppse, 3000, cts.Token);
                                Console.WriteLine($"  SELECT PPSE response:   {Hex(resp)}");

                                // SW1 SW2 상태 코드 해석
                                if (resp.Length >= 2)
                                {
                                    byte sw1 = resp[resp.Length - 2];
                                    byte sw2 = resp[resp.Length - 1];
                                    Console.WriteLine($"  Status word: {sw1:X2} {sw2:X2}  ({InterpretSw(sw1, sw2)})");
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
