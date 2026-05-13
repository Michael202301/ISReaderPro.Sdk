# 11. Bluetooth / BLE 설정

## 개요

IS-3500K에는 BLE(Bluetooth Low Energy) 모듈이 내장되어 있습니다.  
이 API를 통해 BLE 장치 이름, TX 파워, GAP 연결 파라미터 등을 읽고 변경할 수 있습니다.

> **주의:** BLE 이름 변경 등 설정 변경은 리더기 재시작 후에 적용됩니다.

---

## 1. 장치 이름 읽기 / 변경

```csharp
// 현재 BLE 장치 이름 읽기
string name = await reader.BleReadNameAsync();
Console.WriteLine($"BLE Name: {name}");

// 장치 이름 변경
await reader.BleWriteNameAsync("MyReader-001");
Console.WriteLine("이름이 변경되었습니다. 재시작 후 적용됩니다.");

// 검증 읽기
string readback = await reader.BleReadNameAsync();
Console.WriteLine($"Readback: {readback}");
```

---

## 2. MAC 주소 읽기

```csharp
byte[] mac = await reader.BleReadMacAddressAsync();
// MAC은 리틀 엔디언(LE) 순서로 반환되므로 역순으로 표시
string macStr = string.Join(":", mac.Reverse().Select(b => $"{b:X2}"));
Console.WriteLine($"MAC Address: {macStr}");
// 출력 예: "AB:CD:EF:12:34:56"
```

---

## 3. TX 파워 읽기 / 변경

TX 파워는 BLE 송신 강도입니다. 높을수록 도달 거리가 길어지지만 전력 소비가 증가합니다.

```csharp
// TX 파워 읽기
byte[] txPwr = await reader.BleReadTxPowerAsync();
if (txPwr.Length > 0)
{
    string label = IksungReader.TxPowerToString(txPwr[0]);
    Console.WriteLine($"TX Power: {label} (index={txPwr[0]})");
}

// TX 파워 변경 (0~최대 인덱스, 제품마다 다름)
await reader.BleWriteTxPowerAsync(3); // 인덱스 3으로 설정
```

**TxPowerToString 반환값 예시:**

| 인덱스 | 레이블 |
|--------|--------|
| 0 | "-40 dBm" |
| 1 | "-20 dBm" |
| 2 | "-16 dBm" |
| 3 | "-12 dBm" |
| 4 | "-8 dBm" |
| 5 | "-4 dBm" |
| 6 | "0 dBm" |
| 7 | "+4 dBm" |

---

## 4. GAP 연결 파라미터 읽기

```csharp
byte[] gap = await reader.BleReadGapConnectParamsAsync();
if (gap.Length >= 8)
{
    ushort minInterval  = (ushort)((gap[0] << 8) | gap[1]);
    ushort maxInterval  = (ushort)((gap[2] << 8) | gap[3]);
    ushort slaveLatency = (ushort)((gap[4] << 8) | gap[5]);
    ushort supTimeout   = (ushort)((gap[6] << 8) | gap[7]);

    Console.WriteLine($"Min Interval : {minInterval} × 1.25ms = {minInterval * 1.25:F2}ms");
    Console.WriteLine($"Max Interval : {maxInterval} × 1.25ms = {maxInterval * 1.25:F2}ms");
    Console.WriteLine($"Slave Latency: {slaveLatency}");
    Console.WriteLine($"Sup Timeout  : {supTimeout} × 10ms = {supTimeout * 10}ms");
}
```

---

## 5. Central 스캔 (주변 BLE 장치 탐색)

IS-3500K를 BLE Central로 동작시켜 주변 Peripheral 장치를 스캔합니다.

```csharp
// Central 활성화 여부 확인
byte[] centralEn = await reader.BleReadCentralEnableAsync();
bool enabled = centralEn.Length > 0 && centralEn[0] == 1;
Console.WriteLine($"Central Mode: {(enabled ? "Enabled" : "Disabled")}");

// 스캔 시작
await reader.BleCentralScanStartAsync();
Console.WriteLine("스캔 중... (2초)");

await Task.Delay(2000);

// 스캔 중지
await reader.BleCentralScanStopAsync();

// 스캔 결과 목록 가져오기
try
{
    byte[] scanList = await reader.BleCentralScanListAsync(2000);
    if (scanList.Length == 0)
        Console.WriteLine("발견된 장치 없음.");
    else
        Console.WriteLine($"Scan raw: {BitConverter.ToString(scanList).Replace("-", " ")}");
}
catch (IksungProtocolException)
{
    Console.WriteLine("스캔 결과 없음 (미지원 또는 타임아웃).");
}
```

---

## 6. Peripheral 광고 (Advertising)

IS-3500K를 BLE Peripheral로 동작시켜 광고 패킷을 송출합니다.

```csharp
// 광고 시작
await reader.BlePeripheralAdvertisingStartAsync();
Console.WriteLine("BLE 광고 시작.");

await Task.Delay(5000);

// 광고 중지
await reader.BlePeripheralAdvertisingStopAsync();
Console.WriteLine("BLE 광고 중지.");
```

---

## 7. 시스템 리셋

BLE 모듈을 소프트웨어적으로 재시작합니다.

> **주의:** 이 명령 실행 시 리더기와의 연결이 끊어집니다.

```csharp
Console.WriteLine("리셋 후 연결이 끊어집니다...");
await reader.BleSystemResetAsync();
// 이 이후 reader는 사용 불가
```

---

## 8. 전체 설정 조회 예제

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");
Console.WriteLine($"Firmware: {await reader.ReadVersionAsync()}\n");

// BLE 이름
Console.WriteLine($"BLE Name   : {await reader.BleReadNameAsync()}");

// MAC 주소
byte[] mac = await reader.BleReadMacAddressAsync();
Console.WriteLine($"MAC Address: {string.Join(":", mac.Reverse().Select(b => $"{b:X2}"))}");

// TX 파워
byte[] tx = await reader.BleReadTxPowerAsync();
if (tx.Length > 0)
    Console.WriteLine($"TX Power   : {IksungReader.TxPowerToString(tx[0])}");

// GAP 파라미터
byte[] gap = await reader.BleReadGapConnectParamsAsync();
if (gap.Length >= 8)
{
    ushort min = (ushort)((gap[0] << 8) | gap[1]);
    ushort max = (ushort)((gap[2] << 8) | gap[3]);
    Console.WriteLine($"GAP Interval: {min * 1.25:F2}ms ~ {max * 1.25:F2}ms");
}

// Central 상태
byte[] ce = await reader.BleReadCentralEnableAsync();
Console.WriteLine($"Central Mode: {(ce.Length > 0 && ce[0] == 1 ? "Enabled" : "Disabled")}");
```

---

[← ISO 7816 / USIM](10-iso7816-usim.md) | [다음: 릴레이 보드 →](12-relay.md)
