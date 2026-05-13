# 12. 릴레이 보드 (Relay / Digital I/O)

## 개요

IS-3500K의 릴레이 보드는 디지털 입력과 릴레이 출력을 제어합니다.

| 채널 | 방향 | 수량 | 용도 |
|------|------|------|------|
| DIN 1~5 | 입력 | 5채널 | 센서, 스위치, 검출기 연결 |
| RELAY 1~8 | 출력 | 8채널 | 잠금장치, 전등, 경보음 제어 |

---

## 1. 디지털 입력 (DIN) 읽기

### 전체 채널 동시 읽기 (비트마스크)

```csharp
byte[] result = await reader.RelayReadAllInputsAsync();
byte mask = result[0];

// bit0=DIN1, bit1=DIN2, ..., bit4=DIN5
for (byte i = 1; i <= 5; i++)
{
    bool state = IksungReader.GetInputState(mask, i);
    Console.WriteLine($"DIN {i}: {(state ? "HIGH" : "LOW")}");
}
```

### 개별 채널 읽기

```csharp
byte[] result = await reader.RelayReadInputAsync(1); // DIN 1번
bool isHigh = result.Length > 0 && result[0] == 1;
Console.WriteLine($"DIN 1: {(isHigh ? "HIGH" : "LOW")}");
```

### GetInputState 유틸리티

```csharp
byte mask = (await reader.RelayReadAllInputsAsync())[0];

bool din1 = IksungReader.GetInputState(mask, 1);
bool din3 = IksungReader.GetInputState(mask, 3);
```

---

## 2. 릴레이 출력 (RELAY) 읽기

```csharp
byte[] result = await reader.RelayReadAllOutputsAsync();
byte mask = result[0];

// bit0=RELAY1, ..., bit7=RELAY8
for (byte i = 1; i <= 8; i++)
{
    bool on = IksungReader.GetRelayState(mask, i);
    Console.WriteLine($"RELAY {i}: {(on ? "ON" : "OFF")}");
}
```

```csharp
// 특정 릴레이 개별 읽기
byte[] result = await reader.RelayReadOutputAsync(3); // RELAY 3번
bool on = result.Length > 0 && result[0] == 1;
```

---

## 3. 릴레이 출력 제어

### 개별 릴레이 On/Off

```csharp
await reader.RelayWriteOutputAsync(1, true);   // RELAY 1 → ON
await reader.RelayWriteOutputAsync(1, false);  // RELAY 1 → OFF
```

### 전체 릴레이 비트마스크 쓰기

```csharp
// RELAY 1, 3, 5 ON / 나머지 OFF
byte mask = 0b_0001_0101; // bit0=1, bit2=1, bit4=1
await reader.RelayWriteAllOutputsAsync(mask);
```

### 전체 ON / 전체 OFF

```csharp
await reader.RelayAllOnAsync();   // 모든 릴레이 ON
await Task.Delay(1000);
await reader.RelayAllOffAsync();  // 모든 릴레이 OFF
```

---

## 4. Auto-Off 타이머

릴레이를 켠 후 지정한 시간이 지나면 자동으로 꺼지도록 설정합니다.

```csharp
byte relayNo = 1;
ushort delayMs = 2000; // 2초 후 자동 OFF

await reader.RelaySetAutoOffTimeAsync(relayNo, delayMs);
await reader.RelayWriteOutputAsync(relayNo, true);
Console.WriteLine($"RELAY {relayNo} ON → {delayMs}ms 후 자동 OFF");

await Task.Delay(delayMs + 500);

byte[] result = await reader.RelayReadAllOutputsAsync();
bool stillOn = IksungReader.GetRelayState(result[0], relayNo);
Console.WriteLine($"RELAY {relayNo}: {(stillOn ? "아직 ON" : "자동 OFF 완료")}");
```

---

## 5. 입력 실시간 모니터링

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("DIN 입력 모니터링 (Ctrl+C 종료)...\n");

byte prevMask = 0xFF;

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] res = await reader.RelayReadAllInputsAsync(500, cts.Token);
        byte mask = res.Length > 0 ? res[0] : (byte)0;

        if (mask != prevMask)
        {
            string ts = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.Write($"{ts}  ");
            for (byte i = 1; i <= 5; i++)
                Console.Write($"DIN{i}:{(IksungReader.GetInputState(mask, i) ? "H" : "L")} ");
            Console.WriteLine($" (0x{mask:X2})");
            prevMask = mask;
        }

        await Task.Delay(50, cts.Token);
    }
    catch (OperationCanceledException) { break; }
    catch (IksungTimeoutException)     { await Task.Delay(200, cts.Token); }
}
```

---

## 6. 순차 릴레이 제어 예제

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

// 1→8 순차 ON
for (byte i = 1; i <= 8; i++)
{
    await reader.RelayWriteOutputAsync(i, true);
    Console.WriteLine($"RELAY {i} ON");
    await Task.Delay(200);
}

// 전체 OFF
await reader.RelayAllOffAsync();
Console.WriteLine("All OFF");
await Task.Delay(500);

// 8→1 역순 ON
for (byte i = 8; i >= 1; i--)
{
    await reader.RelayWriteOutputAsync(i, true);
    Console.WriteLine($"RELAY {i} ON");
    await Task.Delay(200);
}

// 전체 OFF
await reader.RelayAllOffAsync();
Console.WriteLine("All OFF");
```

---

## 7. API 요약

| 메서드 | 설명 |
|--------|------|
| `RelayReadAllInputsAsync()` | DIN 1~5 전체 비트마스크 읽기 |
| `RelayReadInputAsync(inputNo)` | 특정 DIN 채널 읽기 |
| `RelayReadAllOutputsAsync()` | RELAY 1~8 전체 비트마스크 읽기 |
| `RelayReadOutputAsync(relayNo)` | 특정 RELAY 채널 읽기 |
| `RelayWriteAllOutputsAsync(mask)` | 전체 RELAY 비트마스크로 설정 |
| `RelayWriteOutputAsync(no, on)` | 개별 RELAY On/Off |
| `RelayAllOnAsync()` | 전체 RELAY ON |
| `RelayAllOffAsync()` | 전체 RELAY OFF |
| `RelaySetAutoOffTimeAsync(no, ms)` | Auto-Off 타이머 설정 |
| `GetInputState(mask, inputNo)` | 비트마스크에서 DIN 상태 추출 (static) |
| `GetRelayState(mask, relayNo)` | 비트마스크에서 RELAY 상태 추출 (static) |

---

[← Bluetooth / BLE](11-bluetooth.md) | [다음: 예외 처리 및 고급 사용 →](13-error-handling.md)
