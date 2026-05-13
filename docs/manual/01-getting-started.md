# 01. 시작하기 (Getting Started)

## 1. 패키지 설치

### NuGet 패키지 관리자
```
Install-Package Iksung.Reader
```

### .NET CLI
```bash
dotnet add package Iksung.Reader
```

### 프로젝트 파일 (`.csproj`)
```xml
<ItemGroup>
  <PackageReference Include="Iksung.Reader" Version="0.*" />
</ItemGroup>
```

---

## 2. 리더기 연결

### Serial (UART) 연결
```csharp
using Iksung.Reader;

await using var reader = await IksungReader.ConnectSerialAsync("COM3");
```

**기본 파라미터:**

| 파라미터 | 기본값 | 설명 |
|----------|--------|------|
| `portName` | (필수) | 포트 이름 (Windows: `"COM3"`, Linux: `"/dev/ttyUSB0"`) |
| `baudRate` | `115200` | 통신 속도 |
| `ct` | `default` | 취소 토큰 |

### 포트 이름 찾기

**Windows** — 장치 관리자 > 포트(COM 및 LPT) 에서 확인  
**Linux/macOS** — `ls /dev/tty*` 또는 `ls /dev/cu.*`

---

## 3. 연결 확인

```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

// 펌웨어 버전 출력
string version = await reader.ReadVersionAsync();
Console.WriteLine($"Firmware: {version}");

// 연결 상태 확인
Console.WriteLine($"Connected: {reader.IsConnected}");
Console.WriteLine($"Channel: {reader.ConnectedVia}");
```

---

## 4. 리소스 해제

`IksungReader`는 `IAsyncDisposable`을 구현합니다. `await using` 구문을 사용하면 블록 종료 시 자동으로 연결이 닫힙니다.

```csharp
// 권장: await using 구문
await using var reader = await IksungReader.ConnectSerialAsync("COM3");
// ... 작업 수행 ...
// 블록 종료 시 자동 DisposeAsync() 호출

// 수동 해제가 필요한 경우
var reader = await IksungReader.ConnectSerialAsync("COM3");
try
{
    // ... 작업 수행 ...
}
finally
{
    await reader.DisposeAsync();
}
```

---

## 5. Hello World 예제

```csharp
using Iksung.Reader;
using Iksung.Reader.Exceptions;

Console.WriteLine("[IKSUNG] Connecting...");
await using var reader = await IksungReader.ConnectSerialAsync("COM3");

string fw = await reader.ReadVersionAsync();
Console.WriteLine($"[IKSUNG] Firmware: {fw}");

Console.WriteLine("[IKSUNG] Place a card on the reader...");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
        Console.WriteLine($"Card UID: {BitConverter.ToString(uid).Replace("-", "")}");
        await Task.Delay(500, cts.Token);
    }
    catch (IksungTimeoutException)   { /* 카드 없음 — 계속 대기 */ }
    catch (OperationCanceledException) { break; }
}

Console.WriteLine("[IKSUNG] Done.");
```

---

## 6. 프로젝트 설정 권장사항

### Top-level statements + `await using` 패턴
```csharp
// Program.cs
using Iksung.Reader;

string port = args.Length > 0 ? args[0] : "COM3";
await using var reader = await IksungReader.ConnectSerialAsync(port);
// ...
```

### CancellationToken 적용 (장시간 루프)
```csharp
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    // ... 반복 작업 ...
    try { await Task.Delay(200, cts.Token); }
    catch (OperationCanceledException) { break; }
}
```

### 예외 처리 템플릿
```csharp
try
{
    byte[] uid = await reader.ReadIso14443aUidAsync();
    // ... 처리 ...
}
catch (IksungProtocolException ex)
{
    Console.WriteLine($"프로토콜 오류: {ex.Message}");
}
catch (IksungTimeoutException)
{
    // 카드 없음 또는 응답 없음
}
```

자세한 예외 처리는 [13. 예외 처리 및 고급 사용](13-error-handling.md)을 참조하세요.

---

[← 목차](README.md) | [다음: 공통 명령 →](02-common-commands.md)
