/*
 * Sample 10 – ISO 7816 USIM / Smart Card (.NET Framework 4.x)
 * =============================================================
 * 리더기 내부 USIM 슬롯(또는 외부 스마트카드)을 이용한 ISO 7816 T=0 통신 예제:
 *   1. 카드 활성화 → ATR 수신
 *   2. SELECT MF (Master File) APDU
 *   3. SELECT EF_ICCID (파일 선택)
 *   4. READ BINARY → ICCID 읽기
 *   5. 카드 비활성화
 *
 * ICCID는 SIM 카드의 고유 식별번호 (20자리)입니다.
 *
 * 사용법:
 *   (인수 없이 실행)                리더를 자동으로 찾습니다 — 포트를 몰라도 됩니다
 *   Iso7816.Net4x.exe COM3                      (Serial — Windows)
 *   Iso7816.Net4x.exe COM3 115200 0             (Serial, 채널 0)
 *   Iso7816.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   Iso7816.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace Iso7816.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg = args.Length > 0 ? args[0] : "(자동 탐색)";
            byte   channel  = args.Length > 2 && byte.TryParse(args[2], out byte ch) ? ch : (byte)0;

            // ── 연결: 인수 없으면 자동 탐색 / "COMx" = Serial / "pcsc[:이름]" = PC/SC ──
            IksungReader? reader = await SampleConnect.OpenAsync(args);
            if (reader == null) return;

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
                Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}");
                Console.WriteLine($"[IKSUNG] Channel  : {channel}");
                Console.WriteLine("[IKSUNG] Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            // ── 1. 활성화 → ATR ──
                            Console.WriteLine("─────────────────────────────────────────────────");
                            byte[] atr = await reader.UsimActivateAsync(channel, 3000, cts.Token);
                            Console.WriteLine($"ATR : {Hex(atr)}");
                            Console.WriteLine($"      Protocol: {ParseProtocol(atr)}");

                            // ── 2. SELECT MF (3F 00) ──
                            byte[] selectMf = new byte[] { 0x00, 0xA4, 0x00, 0x00, 0x02, 0x3F, 0x00 };
                            byte[] resp     = await reader.UsimSendTpduAsync(selectMf, channel, 2000, cts.Token);
                            Console.WriteLine($"SELECT MF     : {Hex(resp)}  SW={Sw(resp)}");

                            // ── 3. SELECT EF_ICCID (2F E2) ──
                            byte[] selectEf = new byte[] { 0x00, 0xA4, 0x00, 0x00, 0x02, 0x2F, 0xE2 };
                            resp            = await reader.UsimSendTpduAsync(selectEf, channel, 2000, cts.Token);
                            Console.WriteLine($"SELECT EF_ICCID: {Hex(resp)}  SW={Sw(resp)}");

                            bool canRead = resp.Length >= 2
                                && resp[resp.Length - 2] == 0x90
                                && resp[resp.Length - 1] == 0x00;

                            if (!canRead && resp.Length >= 2 && resp[resp.Length - 2] == 0x9F)
                            {
                                // GET RESPONSE (T=0)
                                byte le         = resp[resp.Length - 1];
                                byte[] getResp  = new byte[] { 0x00, 0xC0, 0x00, 0x00, le };
                                resp            = await reader.UsimSendTpduAsync(getResp, channel, 2000, cts.Token);
                                canRead         = resp.Length >= 2 && resp[resp.Length - 2] == 0x90;
                            }

                            if (canRead)
                            {
                                // ── 4. READ BINARY → ICCID 10바이트 ──
                                byte[] readBin = new byte[] { 0x00, 0xB0, 0x00, 0x00, 0x0A };
                                resp           = await reader.UsimSendTpduAsync(readBin, channel, 2000, cts.Token);
                                Console.WriteLine($"READ BINARY   : {Hex(resp)}  SW={Sw(resp)}");
                                if (resp.Length >= 12 && resp[resp.Length - 2] == 0x90)
                                {
                                    byte[] iccidBcd = new byte[10];
                                    Array.Copy(resp, 0, iccidBcd, 0, 10);
                                    Console.WriteLine($"ICCID (BCD)   : {BcdDecode(iccidBcd)}");
                                }
                            }

                            // ── 5. 비활성화 ──
                            await reader.UsimDeactivateAsync(channel, 1000, cts.Token);
                            Console.WriteLine("Card deactivated.\n");
                        }
                        catch (IksungProtocolException ex) { Console.WriteLine($"Protocol error: {ex.Message}"); }
                        catch (IksungTimeoutException)     { Console.WriteLine("(no card / timeout)"); }
                        catch (OperationCanceledException) { break; }

                        // 5초마다 반복 (USIM은 연속 접근보다 요청 시 접근)
                        try { await Task.Delay(5000, cts.Token); }
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

        static string Hex(byte[] b)  => BitConverter.ToString(b).Replace("-", " ");
        static string Sw(byte[] b)   => b.Length >= 2 ? $"{b[b.Length - 2]:X2}{b[b.Length - 1]:X2}" : "??";

        // ATR 프로토콜 파싱 (간략)
        static string ParseProtocol(byte[] atr)
        {
            if (atr.Length < 2) return "unknown";
            bool hasT1 = atr.Length >= 2 && (atr[1] & 0x0F) == 1;
            return hasT1 ? "T=1" : "T=0";
        }

        // BCD 디코드: 각 니블을 숫자로 변환 (ICCID는 nibble-swap BCD)
        static string BcdDecode(byte[] bcd)
        {
            var sb = new StringBuilder();
            foreach (byte b in bcd)
            {
                sb.Append((char)('0' + (b & 0x0F)));
                sb.Append((char)('0' + (b >> 4)));
            }
            return sb.ToString();
        }
    }
}
