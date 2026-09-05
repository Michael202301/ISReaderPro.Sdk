# 09. AutoRead 이벤트 모드

## 개요

AutoRead 모드는 리더기가 자율적으로 카드를 감지하고, 감지 시 SDK를 통해 이벤트를 발생시킵니다.  
애플리케이션이 폴링 루프를 직접 작성할 필요 없이 이벤트 핸들러만 등록하면 됩니다.

### 폴링 방식 vs AutoRead 비교

| 항목 | 폴링 방식 | AutoRead 방식 |
|------|-----------|--------------|
| CPU 사용 | 루프로 인해 높음 | 이벤트 기반, 낮음 |
| 응답 지연 | 폴링 간격에 따라 다름 | 즉각 |
| 구현 복잡도 | while 루프 직접 작성 | 이벤트 핸들러만 |
| 동시 명령 | 루프 중 다른 명령 불가 | 이벤트 외 명령 가능 |

---

## 1. 기본 사용법

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

// 이벤트 핸들러 등록
reader.TagDetected += (sender, e) =>
{
    Console.WriteLine($"카드 감지!");
    Console.WriteLine($"  CardType : {e.CardType}");
    Console.WriteLine($"  UID      : {e.UidHex}");
};

// AutoRead 시작
await reader.StartAutoReadAsync();
Console.WriteLine("AutoRead 시작. 카드를 올려 주세요. (Enter로 중지)");

Console.ReadLine();

// AutoRead 중지
await reader.StopAutoReadAsync();
Console.WriteLine("AutoRead 중지.");
```

---

## 2. TagDetectedEventArgs 속성

| 속성 | 타입 | 설명 |
|------|------|------|
| `CardType` | `CardType` | 카드 종류 (ISO14443A, MifareClassic 등) |
| `Uid` | `byte[]` | UID 바이트 배열 |
| `UidHex` | `string` | UID 16진수 문자열 (대시 없음) |
| `Cmd1` | `byte` | 프로토콜 Major 명령 바이트 |
| `Cmd2` | `byte` | 프로토콜 Minor 명령 바이트 |
| `RawData` | `byte[]` | 원시 응답 데이터 |

---

## 3. CardType 열거형

```csharp
public enum CardType
{
    Unknown,
    Iso14443a,       // ISO 14443 Type A (일반)
    Iso14443b,       // ISO 14443 Type B
    Iso14443ab,      // Type A 또는 B
    Iso15693,        // ISO 15693 HF 태그
    MifareClassic,   // Mifare Classic 1K/4K
    MifareUltralight,// Mifare Ultralight / NTag
    MifareDesfire,   // Mifare DESFire
    Felica,          // Sony FeliCa
    Lf125Khz,        // LF 125 kHz (EM410X 등)
}
```

---

## 4. 카드 타입별 처리

```csharp
reader.TagDetected += (sender, e) =>
{
    string ts = DateTime.Now.ToString("HH:mm:ss.fff");

    switch (e.CardType)
    {
        case CardType.MifareClassic:
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{ts}  [Mifare Classic]  UID: {e.UidHex}");
            break;

        case CardType.MifareUltralight:
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{ts}  [Ultralight/NTag]  UID: {e.UidHex}");
            break;

        case CardType.MifareDesfire:
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{ts}  [DESFire]  UID: {e.UidHex}");
            break;

        case CardType.Iso15693:
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"{ts}  [ISO 15693]  UID: {e.UidHex}");
            break;

        case CardType.Lf125Khz:
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"{ts}  [LF 125kHz]  UID: {e.UidHex}");
            break;

        default:
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"{ts}  [{e.CardType}]  UID: {e.UidHex}");
            break;
    }
    Console.ResetColor();
};
```

---

## 5. WPF에서 UI 스레드 마샬링

AutoRead 이벤트는 백그라운드 스레드에서 발생합니다.  
WPF UI를 업데이트하려면 `Dispatcher`를 통해 마샬링해야 합니다.

```csharp
reader.TagDetected += (sender, e) =>
{
    // UI 스레드에서 실행
    Application.Current.Dispatcher.Invoke(() =>
    {
        UidTextBox.Text = e.UidHex;
        CardTypeLabel.Content = e.CardType.ToString();
    });
};
```

---

## 6. 이벤트 핸들러 등록 해제

AutoRead를 중지한 후에도 이벤트 구독이 남아 있으면 GC 누수가 발생할 수 있습니다.

```csharp
EventHandler<TagDetectedEventArgs> handler = (sender, e) =>
{
    Console.WriteLine($"UID: {e.UidHex}");
};

reader.TagDetected += handler;
await reader.StartAutoReadAsync();

// ... 잠시 후 ...
await reader.StopAutoReadAsync();
reader.TagDetected -= handler; // 구독 해제
```

---

## 7. AutoRead 중 일반 명령 병행

AutoRead가 실행 중일 때도 일반 명령을 보낼 수 있습니다.  
단, 동시 요청이 겹치지 않도록 주의하세요.

```csharp
await reader.StartAutoReadAsync();

// AutoRead 중 버전 읽기 (가능)
string ver = await reader.ReadVersionAsync();
Console.WriteLine($"Version: {ver}");

await reader.StopAutoReadAsync();
```

---

[← LF 125 kHz](08-lf125khz.md) | [다음: ISO 7816 / USIM →](10-iso7816-usim.md)
