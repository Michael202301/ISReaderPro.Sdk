# 13. 예외 처리 및 고급 사용

## 1. 예외 종류

SDK는 두 가지 전용 예외를 사용합니다.

### IksungProtocolException

리더기가 오류 응답(State ≠ 0)을 반환했을 때 발생합니다.

**원인 예시:**
- 카드가 응답하지 않음 (RF 범위 이탈)
- 인증 실패 (잘못된 키)
- 지원하지 않는 명령
- 카드 Lock 상태에서 쓰기 시도

```csharp
using Iksung.Reader.Exceptions;

try
{
    await reader.MifareAuthenticateAsync(4, MifareKeyType.KeyA, wrongKey);
}
catch (IksungProtocolException ex)
{
    Console.WriteLine($"프로토콜 오류: {ex.Message}");
    // ex.State: 리더기에서 반환된 State 바이트
    Console.WriteLine($"State code: 0x{ex.State:X2}");
}
```

### IksungTimeoutException

지정한 `timeoutMs` 내에 리더기가 응답하지 않을 때 발생합니다.

**원인 예시:**
- 카드가 없음
- 리더기가 응답하지 않음
- `timeoutMs` 값이 너무 짧음

```csharp
try
{
    byte[] uid = await reader.ReadIso14443aUidAsync(500);
}
catch (IksungTimeoutException ex)
{
    // 카드 없음 — 정상적인 상황이므로 보통 무시
    // Console.WriteLine($"타임아웃: {ex.Message}");
}
```

---

## 2. 권장 예외 처리 패턴

### 폴링 루프

```csharp
while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
        // 카드 처리 로직
        await Task.Delay(300, cts.Token);
    }
    catch (IksungTimeoutException)     { /* 카드 없음 — 계속 */ }
    catch (IksungProtocolException ex) { Console.WriteLine($"오류: {ex.Message}"); }
    catch (OperationCanceledException) { break; }
}
```

### 일회성 작업

```csharp
try
{
    byte[] atr = await reader.ActivateMifareAsync();
    await reader.MifareAuthenticateAsync(4, MifareKeyType.KeyA, key);
    byte[] data = await reader.MifareReadBlockAsync(4);
    Console.WriteLine(BitConverter.ToString(data));
}
catch (IksungProtocolException ex)
{
    Console.WriteLine($"카드 오류: {ex.Message}");
}
catch (IksungTimeoutException)
{
    Console.WriteLine("카드를 인식할 수 없습니다.");
}
```

---

## 3. 취소 토큰 (CancellationToken) 사용

모든 async 메서드는 `CancellationToken`을 지원합니다.

```csharp
using var cts = new CancellationTokenSource();

// Ctrl+C 처리
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// 3초 후 자동 취소
using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

try
{
    byte[] uid = await reader.ReadIso14443aUidAsync(2000, timeoutCts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("사용자가 취소하거나 타임아웃되었습니다.");
}
```

---

## 4. Raw 명령 전송

표준 API에 없는 명령을 직접 보낼 때 사용합니다.  
프로토콜 탐색, 신규 펌웨어 기능 테스트에 유용합니다.

```csharp
byte cmd1 = 0x00; // Major: MAJOR_COMMON
byte cmd2 = 0x10; // Minor: READ_VERSION

byte[]? data = null; // 데이터 없음

byte[] response = await reader.SendRawCommandAsync(cmd1, cmd2, data, 1000);
Console.WriteLine($"Response: {BitConverter.ToString(response).Replace("-", " ")}");
```

```csharp
// 데이터 포함 예시
byte[] payload = [0x01, 0x02, 0x03];
byte[] resp = await reader.SendRawCommandAsync(0x02, 0x30, payload, 2000);
```

### 주요 프로토콜 상수 (Cmd1 참조)

| CMD1 (Hex) | 대상 |
|-----------|------|
| `0x00` | 공통 (버전, UID, RF) |
| `0x01` | ISO 14443 A/B |
| `0x02` | Mifare Classic |
| `0x03` | ISO 15693 |
| `0x05` | Mifare Ultralight / NTag |
| `0x09` | DESFire |
| `0x0A` | ISO 7816 / USIM |
| `0x0C` | LF 125 kHz |
| `0x12` | BLE 설정 |
| `0x20` | AutoRead |
| `0x22` | 릴레이 I/O |
| `0x23` | 릴레이 설정 |

---

## 5. 연결 상태 확인

```csharp
Console.WriteLine($"Connected : {reader.IsConnected}");
Console.WriteLine($"Via       : {reader.ConnectedVia}"); // Serial, Socket 등
```

---

## 6. 비동기 디스패치 — WPF / MAUI

이벤트(`TagDetected`)와 async 완료 콜백은 백그라운드 스레드에서 호출됩니다.  
UI 프레임워크의 컨트롤은 UI 스레드에서만 수정 가능합니다.

**WPF:**
```csharp
reader.TagDetected += (s, e) =>
{
    Application.Current?.Dispatcher.Invoke(() =>
    {
        MyTextBlock.Text = e.UidHex;
    });
};
```

**MAUI / Avalonia (MainThread):**
```csharp
reader.TagDetected += (s, e) =>
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        UidLabel.Text = e.UidHex;
    });
};
```

---

## 7. 성능 팁

| 상황 | 권장사항 |
|------|---------|
| 카드 감지만 필요 | `AutoRead` 모드 사용 (폴링 루프보다 CPU 절약) |
| 빠른 연속 읽기 | `timeoutMs`를 짧게 (300~500ms), 딜레이 최소화 |
| 블록 여러 개 읽기 | `Iso15693ReadMultipleBlocksAsync` 또는 `NTagFastReadAsync` 활용 |
| 다중 스레드 | `IksungReader` 하나는 단일 스레드에서 직렬 사용 권장 |
| 정기 구독 | `TagDetected` 이벤트를 사용 후 `-=`로 해제 |

---

[← 릴레이 보드](12-relay.md) | [API 레퍼런스 →](api-reference.md)
