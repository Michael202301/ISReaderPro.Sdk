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

## 3. RF 파워 읽기 / 변경

RF(TX) 파워는 BLE 송신 강도입니다. 높을수록 도달 거리가 길어지지만 전력 소비가 증가합니다.
펌웨어는 **Central / Peripheral 각각의 인덱스**를 2바이트(`[centralIdx][peripheralIdx]`)로 다룹니다.

```csharp
// RF 파워 읽기 (Central / Peripheral 개별)
var (central, peripheral) = await reader.BleReadRfPowerAsync();
Console.WriteLine($"Central={IksungReader.TxPowerToString(central)}, " +
                  $"Peripheral={IksungReader.TxPowerToString(peripheral)}");

// RF 파워 변경 (Central / Peripheral 개별)
await reader.BleWriteRfPowerAsync(centralIndex: 7, peripheralIndex: 7); // 둘 다 +4 dBm

// 호환 API — 단일 인덱스를 양쪽(Central=Peripheral)에 동일 적용
byte[] txPwr = await reader.BleReadTxPowerAsync();   // [0]=Central, [1]=Peripheral
if (txPwr.Length > 0)
    Console.WriteLine($"TX Power: {IksungReader.TxPowerToString(txPwr[0])} (index={txPwr[0]})");
await reader.BleWriteTxPowerAsync(7);                // Central=Peripheral=+4 dBm
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

## 9. 연결 상태 진단 (펌웨어 V1.37 §12)

Central / Peripheral 의 BLE 연결 상태를 2-byte `[state][tx_ready]` 로 조회합니다.

```csharp
BleConnectState cs = await reader.BleReadCentralConnectStateAsync();
Console.WriteLine(cs);            // State=0x03, TxReady=True, Connected=True
if (cs.TxReady)
    Console.WriteLine("데이터 전송 가능");
```

**state 값:**

| state | 의미 |
|-------|------|
| `0x00` | 미연결 (idle) |
| `0x01` | 페어링/GATT 진행 중 |
| `0x02` | MTU 교환 진행 중 (**아직 전송 불가**) |
| `0x03` | 완전 준비 (TX 가능) |
| `0xFE` | 에러 (마지막 페어링 실패) |

| tx_ready | 의미 |
|----------|------|
| `0` | 전송 불가 |
| `1` | 즉시 전송 가능 — **호스트는 이 값만 보면 됨** |

> ⚠ **V1.36 → V1.37 의미 변화**: V1.36 에서는 Central `state==2` 가 "전송 가능" 이었으나,
> V1.37 부터 `state==2` 는 "MTU 교환 중(전송 불가)" 입니다. 반드시 `TxReady == true`
> (또는 `State == 3`) 로 전송 가능 여부를 판단하세요. (1-byte 응답이면 SDK 가 V1.36
> 규칙으로 하위호환 파싱합니다.)

---

## 10. 시스템 리셋 범위 (펌웨어 V1.37 §11)

`BleResetScope` 로 재시작 범위를 지정할 수 있습니다. 무인자 `BleSystemResetAsync()` 는 Full reset 하위호환입니다.

```csharp
// 설정만 재로드 (~10ms, 연결 유지)
await reader.BleSystemResetAsync(BleResetScope.SettingsReload);

// Central 만 재시작 (~100ms, Peripheral 연결 유지)
await reader.BleSystemResetAsync(BleResetScope.CentralOnly);

// 전체 재부팅 (~1s, 모든 연결 해제 → 재연결 필요)
await reader.BleSystemResetAsync(BleResetScope.Full);
```

| scope | 값 | 동작 |
|-------|----|------|
| `Full` | 0x00 | MCU 전체 재부팅 (~1s). 모든 BLE 연결 해제, 재연결 필요 |
| `CentralOnly` | 0x01 | Central 만 재시작 (~100ms) |
| `PeripheralOnly` | 0x02 | Peripheral 만 재시작 (~100ms) |
| `SettingsReload` | 0x03 | EEPROM→RAM 재로드만 (~10ms) |

> 정의되지 않은 scope 를 보내면 펌웨어가 `state=0xFF` 로 응답하며 `IksungProtocolException` 이 발생합니다.

---

## 11. Boot Health 진단 (펌웨어 V1.40 §13)

부팅 시 16개 핵심 subsystem 의 초기화 결과를 조회합니다.

```csharp
BleBootHealth health = await reader.BleReadBootHealthAsync();
if (health.AllOk)
    Console.WriteLine("모든 subsystem 정상");
else
    Console.WriteLine($"실패: {string.Join(", ", health.FailedSubsystems)}");

// 개별 결과
for (int i = 0; i < health.Results.Count; i++)
    Console.WriteLine($"{BleBootHealth.Names[i]}: {(health.Results[i] == 0 ? "OK" : $"FAIL({health.Results[i]})")}");
```

각 값은 `int8`: `0`=OK, `!=0`=실패코드. subsystem 인덱스(0~15):

| # | 이름 | # | 이름 |
|---|------|---|------|
| 0 | eeprom | 8 | ble_auth_key |
| 1 | eeprom_ble | 9 | tim_tick |
| 2 | eeprom_acu | 10 | led |
| 3 | wdt | 11 | pn5180 |
| 4 | ble | 12 | desfire |
| 5 | uart0 | 13 | usb |
| 6 | uart1 | 14 | wiegand |
| 7 | crypto | 15 | threads |

---

## 12. 설정 쓰기 + 범위 검증 (펌웨어 V1.37 §10)

쓰기 API 는 송신 전 클라이언트 측에서 범위를 검증하고(`ArgumentOutOfRangeException`),
펌웨어가 거부하면 `state=0xFF` → `IksungProtocolException` 을 던집니다.

```csharp
// GAP 연결 파라미터 (모두 16-bit, 펌웨어는 LE 로 받음)
await reader.BleWriteGapConnectParamsAsync(
    minConnInterval: 16,   // 6~3200 (×1.25ms)
    maxConnInterval: 60,   // 6~3200, min≤max
    slaveLatency:    0,    // 0~499
    connSupTimeout:  320); // 10~3200 (×10ms)
// 추가 규칙: connSupTimeout×4 > (1+slaveLatency)×maxConnInterval

// 광고 간격 (BE, 20~10240ms)
int adv = await reader.BleReadAdvIntervalAsync();
await reader.BleWriteAdvIntervalAsync(160);

// 패킷 수신 타임아웃 (BE, 1~10000ms)
int rt = await reader.BleReadReceivedTimeoutAsync();
await reader.BleWriteReceivedTimeoutAsync(200);

// No-Protocol(raw passthrough) 모드 (timeout BE, 1~10000ms)
var (en, to) = await reader.BleReadCentralNoProtocolAsync();
await reader.BleWriteCentralNoProtocolAsync(enable: true, noProtocolTimeoutMs: 200);
await reader.BleWritePeripheralNoProtocolAsync(enable: false, noProtocolTimeoutMs: 200);
```

**검증 범위 요약:**

| Write | 필드 | Endian | 범위 |
|-------|------|:------:|------|
| GAP `0x13` | min/max conn interval | LE | 6~3200 (×1.25ms), min≤max |
| GAP `0x13` | slave_latency | LE | 0~499 |
| GAP `0x13` | conn_sup_timeout | LE | 10~3200 (×10ms) |
| Adv `0x3B` | adv interval | **BE** | 20~10240 ms |
| Recv `0x53` | recv timeout | **BE** | 1~10000 ms |
| NoProto `0x56/0x58` | timeout | **BE** | 1~10000 ms |

---

## 13. 기타 설정 R/W

```csharp
// 출력 인터페이스 (bit0=RS232 / bit1=USB→Serial)
byte iface = await reader.BleReadOutputInterfaceAsync();
await reader.BleWriteOutputInterfaceAsync(0x01);

// PHY (0=Auto / 1=1M / 2=2M)
byte phy = await reader.BleReadPhysAsync();
await reader.BleWritePhysAsync(0);

// Central / Peripheral 모드
await reader.BleWriteCentralEnableAsync(true);
byte mode = await reader.BleReadPeripheralEnableAsync();   // 0=Disable / 1=SPP
await reader.BleWritePeripheralEnableAsync(1);

// RSSI (dBm, sbyte)
sbyte rssi = await reader.BleReadCentralConnectedRssiAsync();

// 연결 해제
await reader.BleCentralUuidDisconnectAsync();
await reader.BlePeripheralDisconnectAsync();

// Bluetooth Card
bool using_ = await reader.BleReadBleCardUsingAsync();
await reader.BleWriteBleCardUsingAsync(true);
byte cardRssi = await reader.BleReadBleCardRssiAsync();
await reader.BleWriteBleCardRssiAsync(80);   // 절대값 0~200
```

---

## 14. 보안 모델 / 인증 키 신뢰 경계 (펌웨어 V1.41 §14)

- BLE 인증 키(`ChallengeResponseAESKey`, 16B)는 **공장 출고 시 랜덤 생성**되어 EEPROM 에
  저장됩니다(와이파이 공유기 라벨 모델).
- **USB/UART (물리, 신뢰 채널)**: `0x1D` 응답에 AES 키 **포함** — 로컬 채널이면 정상적으로 키를 받습니다.
- **BLE (무선, 비신뢰 채널)**: 키 응답에서 제외 + 인증 없이는 명령 거부.
- **인증 우회 허용 명령** (인증 전에도 통과): `0x64`(challenge) / `0x65`(auth) / `0x66`(auth state)
  / `0x40`·`0x41`(data relay). 단 `0x40/0x41` 페이로드는 무인증 통과되므로 **응용 레이어 자체 암호화를 권장**합니다.

> **제거됨 (V1.41)**: `0x62/0x63` USER_SECURITY_LEVELS R/W 는 펌웨어에서 제거되었습니다(수신 시
> 무응답 drop). 대체 경로 — protocol+key: `0x1D/0x1E`, output iface: `0x19/0x1A`, timeout: `0x52/0x53`.
> SDK 는 신규 API 를 제공하지 않으며, 내부 상수 `BLE_CFG_USER_SECURITY_LEVELS_*` 는 deprecated 입니다.

---

## 15. UUID / 연결 방식 / 데이터 송신 / 보안 인증

```csharp
// ── UUID 5필드 R/W (20 byte: RxD2+TxD2+Base2+2+12) ──
byte[] cUuid = await reader.BleReadCentralUuidAsync();
await reader.BleWriteCentralUuidAsync(cUuid);
byte[] pUuid = await reader.BleReadPeripheralUuidAsync();
await reader.BleWritePeripheralUuidAsync(pUuid);

// ── Central 연결 방식 (0=UUID 자동 / 1=User / 2=MAC 자동) ──
var (type, mac) = await reader.BleReadCentralConnectTypeAsync();
await reader.BleWriteCentralConnectTypeAsync(0);                       // UUID 매칭 자동
await reader.BleWriteCentralConnectTypeAsync(2, new byte[]{1,2,3,4,5,6}); // MAC 매칭 자동
await reader.BleCentralMatchedConnectAsync(new byte[]{1,2,3,4,5,6});  // 스캔 목록 MAC 연결

// ── 데이터 송신 — Send Command(프로토콜 wrap) / Send Data(raw) ──
await reader.BleCentralSendCommandAsync(payload);     // → Peripheral, 프로토콜 wrap (0x2B)
await reader.BleCentralSendDataAsync(payload);        // → Peripheral, raw (0x40)
await reader.BlePeripheralSendCommandAsync(payload);  // → Central, 프로토콜 wrap (0x35)
await reader.BlePeripheralSendDataAsync(payload);     // → Central, raw (0x41)

// ── 보안 레벨 (1=암호화 / 2=인증암호화 / 3=Key matching / 4=LE Secure) ──
var (level, key) = await reader.BleReadSecurityLevelAsync();
await reader.BleWriteSecurityLevelAsync(2);                    // 1 또는 2
await reader.BleWriteSecurityLevelKeyMatchingAsync("123456");  // 레벨 3, 6자리 ASCII
await reader.BleWriteSecurityLevelLeSecureAsync(oobKey16);     // 레벨 4, 16 byte OOB

// ── 통신 데이터(내부 명령 사용) + master key ──
var (enable, aesKey) = await reader.BleReadCommunicationDataAsync(); // aesKey 는 USB/UART 응답에만 포함
await reader.BleWriteCommunicationDataAsync(true, masterKey16);
await reader.BleWriteCommunicationDataAsync(false);

// ── Challenge-Response 인증 ──
byte[] challenge = await reader.BleSecurityGetRandomAsync();   // 16 byte
// response 32 byte = master key 로 계산
await reader.BleSecurityAuthenticateAsync(response32);
bool authed = await reader.BleReadSecurityAuthStateAsync();

// ── Bluetooth Card 키 저장 (Save 전용, Read 없음) ──
await reader.BleWriteBleCardKeyAsync(firstKey16, customUuid16);
```

> **검증** — UUID 20 byte, OOB 16 byte, response 32 byte, MAC 6 byte, key matching 6자리 ASCII,
> master key/카드 키 16 byte 등 길이가 맞지 않으면 송신 전 `ArgumentException` 이 발생합니다.

---

[← ISO 7816 / USIM](10-iso7816-usim.md) | [다음: 릴레이 보드 →](12-relay.md)
