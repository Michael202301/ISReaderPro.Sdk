/*
 * Sample 12 – Relay / Digital I/O (.NET Framework 4.x)
 * =======================================================
 * IS-3500K의 릴레이 모듈 제어 예제:
 *   1. 모든 입력 핀(DIN 1~5) 상태 읽기
 *   2. 모든 릴레이 출력(RELAY 1~8) 상태 읽기
 *   3. 릴레이 순차 ON (1→2→3→…→8, 300ms 간격)
 *   4. 모든 릴레이 OFF
 *   5. 릴레이 역순 ON (8→7→6→…→1, 300ms 간격)
 *   6. 전체 ON / 전체 OFF 토글
 *   7. 자동 꺼짐 타이머 설정 예시 (Relay 1, 2000ms)
 *   8. 입력 실시간 모니터링 루프
 *
 * 사용법:
 *   Relay.Net4x.exe COM3                      (Serial — Windows)
 *   Relay.Net4x.exe COM3 115200 monitor       (Serial, 입력 모니터링 전용)
 *   Relay.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   Relay.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace Relay.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg  = args.Length > 0 ? args[0] : "COM3";
            bool monitorOnly = args.Length > 2 && args[2].Equals("monitor", StringComparison.OrdinalIgnoreCase);

            IksungReader reader;
            if (firstArg.StartsWith("pcsc", StringComparison.OrdinalIgnoreCase))
            {
                string? readerName = firstArg.Contains(":")
                    ? firstArg.Substring(firstArg.IndexOf(':') + 1).Trim()
                    : null;
                if (readerName == null)
                {
                    var readers = IksungPcscDiscovery.GetAvailableReaders();
                    if (readers.Count == 0)
                    {
                        Console.WriteLine("[ERROR] PC/SC 리더를 찾을 수 없습니다.");
                        Console.WriteLine("  - USB CCID 리더(IS-3500Z 등)가 연결되어 있는지 확인하세요.");
                        Console.WriteLine("  - Windows Smart Card Service(SCardSvr)가 실행 중인지 확인하세요.");
                        return;
                    }
                    readerName = readers[0];
                    Console.WriteLine("[IKSUNG] PC/SC 리더 자동 선택: " + readerName);
                }
                else
                {
                    Console.WriteLine("[IKSUNG] PC/SC 연결: " + readerName);
                }
                reader = await IksungReader.ConnectPcscAsync(readerName);
            }
            else
            {
                Console.WriteLine("[IKSUNG] Serial 연결: " + firstArg);
                reader = await IksungReader.ConnectSerialAsync(firstArg);
            }

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
                Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}\n");

                try
                {
                    if (monitorOnly)
                    {
                        await RunMonitorLoop(reader);
                        return;
                    }

                    // ── 1. 모든 입력 핀 상태 ──
                    Console.WriteLine("── DIN (Digital Input) 1~5 ────────────────────");
                    byte inputMask = (await reader.RelayReadAllInputsAsync())[0];
                    for (byte i = 1; i <= 5; i++)
                    {
                        bool state = IksungReader.GetInputState(inputMask, i);
                        Console.WriteLine($"  DIN {i} : {(state ? "HIGH ●" : "LOW  ○")}");
                    }

                    // ── 2. 모든 릴레이 출력 상태 ──
                    Console.WriteLine("\n── RELAY Output 1~8 ────────────────────────────");
                    byte relayMask = (await reader.RelayReadAllOutputsAsync())[0];
                    for (byte i = 1; i <= 8; i++)
                    {
                        bool state = IksungReader.GetRelayState(relayMask, i);
                        Console.WriteLine($"  RELAY {i} : {(state ? "ON  ●" : "OFF ○")}");
                    }

                    // ── 3. 릴레이 순차 ON (1→8) ──
                    Console.WriteLine("\n── Sequential ON (RELAY 1 → 8) ─────────────────");
                    for (byte i = 1; i <= 8; i++)
                    {
                        await reader.RelayWriteOutputAsync(i, true);
                        Console.WriteLine($"  RELAY {i} → ON");
                        await Task.Delay(300);
                    }

                    // ── 4. 전체 OFF ──
                    Console.WriteLine("\n── All RELAY OFF ────────────────────────────────");
                    await reader.RelayAllOffAsync();
                    Console.WriteLine("  All relays turned OFF.");
                    await Task.Delay(500);

                    // ── 5. 릴레이 역순 ON (8→1) ──
                    Console.WriteLine("\n── Sequential ON (RELAY 8 → 1) ─────────────────");
                    for (byte i = 8; i >= 1; i--)
                    {
                        await reader.RelayWriteOutputAsync(i, true);
                        Console.WriteLine($"  RELAY {i} → ON");
                        await Task.Delay(300);
                    }

                    // ── 6. 전체 OFF ──
                    Console.WriteLine("\n── All RELAY OFF ────────────────────────────────");
                    await reader.RelayAllOffAsync();
                    Console.WriteLine("  All relays turned OFF.");
                    await Task.Delay(500);

                    // ── 7. 전체 ON → 잠시 대기 → 전체 OFF ──
                    Console.WriteLine("\n── All ON → 1s → All OFF ────────────────────────");
                    await reader.RelayAllOnAsync();
                    Console.WriteLine("  All relays → ON");
                    await Task.Delay(1000);
                    await reader.RelayAllOffAsync();
                    Console.WriteLine("  All relays → OFF");
                    await Task.Delay(500);

                    // ── 8. 자동 꺼짐 타이머 (RELAY 1 : 2000ms 후 자동 OFF) ──
                    Console.WriteLine("\n── Auto-Off Timer: RELAY 1 for 2000ms ───────────");
                    await reader.RelaySetAutoOffTimeAsync(1, 2000);
                    await reader.RelayWriteOutputAsync(1, true);
                    Console.WriteLine("  RELAY 1 → ON (will auto-off in ~2s)");
                    await Task.Delay(2500);
                    byte afterMask     = (await reader.RelayReadAllOutputsAsync())[0];
                    bool relay1State   = IksungReader.GetRelayState(afterMask, 1);
                    Console.WriteLine($"  RELAY 1 state after 2.5s : {(relay1State ? "ON (not auto-off?)" : "OFF ✓")}");

                    // ── 9. 입력 핀 비트마스크 직접 표시 ──
                    Console.WriteLine("\n── Input bitmask (raw) ──────────────────────────");
                    byte rawInput = (await reader.RelayReadAllInputsAsync())[0];
                    Console.WriteLine($"  Raw byte : 0x{rawInput:X2}  (binary: {ToBinary8(rawInput)})");

                    Console.WriteLine("\n[IKSUNG] Relay control complete.");
                    Console.WriteLine("\nPress any key to start input monitoring (Ctrl+C to exit)...");
                    Console.ReadKey(true);

                    await RunMonitorLoop(reader);
                }
                catch (IksungProtocolException ex) { Console.WriteLine($"Error: {ex.Message}"); }
                catch (IksungTimeoutException ex)  { Console.WriteLine($"Timeout: {ex.Message}"); }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        // ── 입력 모니터링 루프 ──
        static async Task RunMonitorLoop(IksungReader reader)
        {
            Console.WriteLine("\n[IKSUNG] Monitoring DIN inputs (Ctrl+C to exit)...\n");
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                byte prevMask = 0xFF; // 초기값을 강제 출력 트리거로 사용
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        byte[] result = await reader.RelayReadAllInputsAsync(500, cts.Token);
                        byte mask = result.Length > 0 ? result[0] : (byte)0;

                        if (mask != prevMask)
                        {
                            string ts = DateTime.Now.ToString("HH:mm:ss.fff");
                            Console.Write($"{ts}  DIN: ");
                            for (byte i = 1; i <= 5; i++)
                            {
                                bool state = IksungReader.GetInputState(mask, i);
                                Console.Write($"[{i}:{(state ? "H" : "L")}] ");
                            }
                            Console.WriteLine($" (0x{mask:X2})");
                            prevMask = mask;
                        }

                        await Task.Delay(50, cts.Token);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (IksungTimeoutException)     { await Task.Delay(200, cts.Token); }
                }
            }

            Console.WriteLine("\n[IKSUNG] Monitoring stopped.");
        }

        static string ToBinary8(byte b)
        {
            var sb = new StringBuilder();
            for (int i = 7; i >= 0; i--)
                sb.Append((b >> i) & 1);
            return sb.ToString();
        }
    }
}
