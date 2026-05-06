/*
 * Sample 11 – Bluetooth / BLE 설정
 * ===================================
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
 *   dotnet run -- COM3
 *   dotnet run -- COM3 115200 "MyReader"    (이름 변경 후 복원)
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

string portName  = args.Length > 0 ? args[0] : "COM3";
string? newName  = args.Length > 2 ? args[2] : null;

Console.WriteLine($"[IKSUNG] Connecting to {portName}...");
await using var reader = await IksungReader.ConnectSerialAsync(portName);
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
        Console.WriteLine($"\nChanging BLE name: \"{currentName}\" → \"{newName}\"");
        await reader.BleWriteNameAsync(newName);
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
