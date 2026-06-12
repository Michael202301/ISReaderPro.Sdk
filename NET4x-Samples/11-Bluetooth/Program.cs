/*
 * Sample 11 – Bluetooth / BLE 설정 (.NET Framework 4.x)
 * ========================================================
 * IS-3500K의 BLE 모듈 설정을 읽고 변경하는 예제:
 *   1. 현재 BLE 장치 이름 읽기
 *   2. MAC 주소 읽기
 *   3. TX 파워 읽기
 *   4. BLE 이름 변경 (원래 이름으로 복원)
 *   5. Central 스캔 시작/중지 + 주변 장치 목록
 *   6. (주석) 시스템 리셋
 *
 * 주의:
 *   - BLE 이름 변경은 리더기 재시작 후 적용됩니다.
 *   - BleSystemResetAsync() 호출 시 연결이 끊어집니다.
 *
 * 사용법:
 *   Bluetooth.Net4x.exe COM3                      (Serial — Windows)
 *   Bluetooth.Net4x.exe COM3 115200 "MyReader"    (Serial, 이름 변경 후 복원)
 *   Bluetooth.Net4x.exe pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   Bluetooth.Net4x.exe "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
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
using System.Linq;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace Bluetooth.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
            string firstArg = args.Length > 0 ? args[0] : "COM3";
            string? newName = args.Length > 2 ? args[2] : null;

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
                    // ── 1. 현재 BLE 이름 ──
                    string currentName = await reader.BleReadNameAsync();
                    Console.WriteLine($"BLE Device Name  : \"{currentName}\"");

                    // ── 2. MAC 주소 ──
                    byte[] mac = await reader.BleReadMacAddressAsync();
                    // MAC은 LE 바이트 순서 → 표시 시 역순
                    string macStr = string.Join(":", mac.Reverse().Select(b => $"{b:X2}"));
                    Console.WriteLine($"MAC Address      : {macStr}");

                    // ── 3. TX 파워 ──
                    byte[] txPwr = await reader.BleReadTxPowerAsync();
                    if (txPwr.Length > 0)
                        Console.WriteLine($"TX Power         : {IksungReader.TxPowerToString(txPwr[0])}  (index={txPwr[0]})");

                    // ── 4. GAP 연결 파라미터 ──
                    byte[] gap = await reader.BleReadGapConnectParamsAsync();
                    if (gap.Length >= 8)
                    {
                        ushort minInterval  = (ushort)((gap[0] << 8) | gap[1]);
                        ushort maxInterval  = (ushort)((gap[2] << 8) | gap[3]);
                        ushort slaveLatency = (ushort)((gap[4] << 8) | gap[5]);
                        ushort supTimeout   = (ushort)((gap[6] << 8) | gap[7]);
                        Console.WriteLine($"GAP Min Interval : {minInterval} (×1.25ms = {minInterval * 1.25:F2}ms)");
                        Console.WriteLine($"GAP Max Interval : {maxInterval} (×1.25ms = {maxInterval * 1.25:F2}ms)");
                        Console.WriteLine($"Slave Latency    : {slaveLatency}");
                        Console.WriteLine($"Sup Timeout      : {supTimeout} (×10ms = {supTimeout * 10}ms)");
                    }

                    // ── 5. Central Enable 상태 ──
                    byte[] centralEn = await reader.BleReadCentralEnableAsync();
                    Console.WriteLine($"Central Mode     : {(centralEn.Length > 0 && centralEn[0] == 1 ? "Enabled" : "Disabled")}");

                    // ── 6. BLE 이름 변경 (테스트) ──
                    if (!string.IsNullOrEmpty(newName))
                    {
                        string nameToWrite = newName!;
                        Console.WriteLine($"\nChanging BLE name: \"{currentName}\" → \"{nameToWrite}\"");
                        await reader.BleWriteNameAsync(nameToWrite);
                        Console.WriteLine("  Name written to Flash. (takes effect after reboot)");

                        // 검증 읽기
                        string readback = await reader.BleReadNameAsync();
                        Console.WriteLine($"  Readback: \"{readback}\"");

                        // 원래 이름으로 복원
                        Console.WriteLine($"  Restoring original name: \"{currentName}\"");
                        await reader.BleWriteNameAsync(currentName);
                        string restored = await reader.BleReadNameAsync();
                        Console.WriteLine($"  Restored: \"{restored}\"");
                    }

                    // ── 7. Central 스캔 (2초) ──
                    Console.WriteLine("\nStarting BLE Central scan (2 seconds)...");
                    await reader.BleCentralScanStartAsync();
                    await Task.Delay(2000);
                    await reader.BleCentralScanStopAsync();
                    Console.WriteLine("Scan stopped.");

                    try
                    {
                        byte[] scanList = await reader.BleCentralScanListAsync(2000);
                        if (scanList.Length == 0)
                            Console.WriteLine("Scan result      : (no devices found)");
                        else
                            Console.WriteLine($"Scan result raw  : {BitConverter.ToString(scanList).Replace("-", " ")}");
                    }
                    catch (IksungProtocolException) { Console.WriteLine("Scan result      : (unavailable)"); }

                    /*
                     * ── 8. 시스템 리셋 (주석 해제 시 BLE 모듈 재시작) ──
                     * Console.WriteLine("\nSystem reset...");
                     * await reader.BleSystemResetAsync();
                     * Console.WriteLine("Reset sent. Connection will drop.");
                     */

                    Console.WriteLine("\n[IKSUNG] BLE configuration complete.");
                }
                catch (IksungProtocolException ex) { Console.WriteLine($"Error: {ex.Message}"); }
                catch (IksungTimeoutException ex)  { Console.WriteLine($"Timeout: {ex.Message}"); }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }
    }
}
