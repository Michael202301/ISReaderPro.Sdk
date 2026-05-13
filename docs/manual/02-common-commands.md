# 02. 공통 명령 (Common Commands)

리더기 자체 정보를 읽고 RF 출력을 제어하는 기본 명령들입니다.

---

## 1. 펌웨어 버전 읽기

```csharp
string version = await reader.ReadVersionAsync();
Console.WriteLine($"Firmware: {version}");
// 출력 예: "Firmware: IS-3500K V1.8.0"
```

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| `timeoutMs` | `1000` | 응답 대기 최대 시간 (ms) |
| `ct` | `default` | 취소 토큰 |

---

## 2. 리더기 고유 ID 읽기

리더기 하드웨어에 고유하게 부여된 식별자(시리얼 번호)를 읽습니다.

```csharp
byte[] uid = await reader.ReadUniqueIdAsync();
string uidHex = BitConverter.ToString(uid).Replace("-", "");
Console.WriteLine($"Reader UID: {uidHex}");
```

---

## 3. RF 안테나 제어

### RF 켜기
```csharp
await reader.RfOnAsync();
```

### RF 끄기
```csharp
await reader.RfOffAsync();
```

> **주의:** RF를 끄면 카드와의 통신이 즉시 중단됩니다. 연속 읽기 루프 사이에 RF를 껐다 켜면 카드 중복 감지를 방지할 수 있습니다.

---

## 4. 빠른 UID 읽기 (카드 타입별)

상세 활성화 없이 UID만 빠르게 읽을 때 사용합니다.

```csharp
// ISO 14443 Type A (Mifare 계열 포함)
byte[] uidA = await reader.ReadIso14443aUidAsync();

// ISO 14443 Type B
byte[] uidB = await reader.ReadIso14443bUidAsync();

// ISO 15693 (ICODE 계열 등 HF 스티커 태그)
byte[] uid15 = await reader.ReadIso15693UidAsync();

// LF 125 kHz (EM410X 등)
byte[] uidLf = await reader.ReadLf125KhzUidAsync();
```

> 카드가 없으면 `IksungTimeoutException`이 발생합니다.

---

## 5. 전형적인 폴링 루프

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}  UID: {BitConverter.ToString(uid).Replace("-", "")}");
        await Task.Delay(300, cts.Token); // 중복 감지 방지 딜레이
    }
    catch (IksungTimeoutException)     { /* 카드 없음 */ }
    catch (OperationCanceledException) { break; }
}
```

---

[← 시작하기](01-getting-started.md) | [다음: ISO 14443 A/B →](03-iso14443ab.md)
