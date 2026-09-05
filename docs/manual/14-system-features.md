# 14. 시스템 레벨 기능

연결 생명주기 관리, 상태 이벤트, 진단, 포트 탐색, Raw 패킷 모니터링 등 SDK의 시스템 레벨 기능을 설명합니다.

---

## 목차

| 기능 | 클래스/멤버 |
|------|------------|
| [A. 포트 탐색](#a-포트-탐색) | `IksungReaderDiscovery` |
| [B. 연결 옵션](#b-연결-옵션) | `IksungReaderOptions` |
| [C. 연결 상태 이벤트](#c-연결-상태-이벤트) | `reader.ConnectionChanged` |
| [D. 리더기 정보 조회](#d-리더기-정보-조회) | `reader.GetReaderInfoAsync`, `reader.PingAsync` |
| [E. Raw 패킷 모니터링](#e-raw-패킷-모니터링) | `reader.RawPacketReceived` |
| [F. 자동 재연결](#f-자동-재연결) | `IksungReaderOptions.AutoReconnect` |
| [G. 명시적 연결 해제](#g-명시적-연결-해제) | `reader.DisconnectAsync` |

---

## A. 포트 탐색

### `IksungReaderDiscovery.GetAllSerialPorts()`

시스템에 등록된 모든 시리얼 포트 이름을 반환합니다. `System.IO.Ports.SerialPort.GetPortNames()` 래핑입니다.

```csharp
IEnumerable<string> ports = IksungReaderDiscovery.GetAllSerialPorts();
foreach (var p in ports)
    Console.WriteLine(p);  // COM3, COM4, ...
```

### `IksungReaderDiscovery.ScanIksungPortsAsync()`

각 포트에 Version 패킷을 전송하여 **Iksung 프로토콜로 응답하는 포트만** 반환합니다.  
최대 4개 포트를 병렬로 테스트하므로 빠릅니다.

```csharp
var progress = new Progress<(string portName, bool isIksung)>(p =>
    Console.WriteLine($"{p.portName}: {(p.isIksung ? "✓ Iksung" : "✗")}"));

IReadOnlyList<string> found = await IksungReaderDiscovery.ScanIksungPortsAsync(
    baudRate:         115200,   // 기본값
    pingTimeoutMs:    400,      // 포트당 대기 시간
    progressCallback: progress);

Console.WriteLine($"발견된 포트: {string.Join(", ", found)}");
```

### `IksungReaderDiscovery.PingPortAsync()`

단일 포트에 Version 패킷을 보내 Iksung 리더기인지 확인합니다.  
이미 다른 프로세스가 점유 중인 포트는 예외 없이 `false`를 반환합니다.

```csharp
bool isIksung = await IksungReaderDiscovery.PingPortAsync("COM3", baudRate: 115200);
Console.WriteLine(isIksung ? "Iksung 리더기" : "응답 없음");
```

---

## B. 연결 옵션

`IksungReaderOptions` 클래스로 연결 동작을 제어합니다.

```csharp
var options = new IksungReaderOptions
{
    AutoReconnect    = true,    // 비정상 끊김 시 자동 재연결
    ReconnectDelayMs = 2000,    // 재시도 간격 (ms)
    DefaultTimeoutMs = 1000,    // PingAsync 등 기본 타임아웃
    LogRawPackets    = false,   // RawPacketReceived 이벤트 발생 여부
};
```

### 팩토리 메서드에 전달

```csharp
// 연결과 동시에 옵션 적용
await using var reader = await IksungReader.ConnectSerialAsync("COM3", 115200, options);
```

### 연결 후 적용

```csharp
// 기존 연결 유지하면서 옵션 변경
await using var reader = await IksungReader.ConnectSerialAsync("COM3");
reader.ApplyOptions(new IksungReaderOptions { AutoReconnect = true });
```

| 속성 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| `AutoReconnect` | `bool` | `false` | 비정상 끊김 시 자동 재연결 |
| `ReconnectDelayMs` | `int` | `2000` | 재연결 시도 간격 (ms) |
| `DefaultTimeoutMs` | `int` | `1000` | `PingAsync` 등 기본 타임아웃 |
| `LogRawPackets` | `bool` | `false` | `RawPacketReceived` 이벤트 활성화 |

---

## C. 연결 상태 이벤트

`reader.ConnectionChanged` 이벤트는 연결/끊김 시 발생합니다.

```csharp
reader.ConnectionChanged += (sender, isConnected) =>
{
    if (isConnected)
        Console.WriteLine("✓ 연결됨");
    else
        Console.WriteLine("✗ 연결 끊김 — 재연결 시도 중...");
};
```

> **주의**: 이벤트 핸들러는 **백그라운드 스레드**에서 호출됩니다.  
> WinForms/WPF에서 UI 컨트롤에 접근하려면 `Invoke`/`Dispatcher.Invoke`가 필요합니다.

### WinForms 예제

```csharp
// .NET Framework 4.x 이상
reader.ConnectionChanged += (sender, isConnected) =>
{
    if (InvokeRequired)
        Invoke(new Action(() => UpdateStatus(isConnected)));
    else
        UpdateStatus(isConnected);
};
```

### WPF 예제

```csharp
reader.ConnectionChanged += (sender, isConnected) =>
    Application.Current.Dispatcher.Invoke(() => UpdateStatus(isConnected));
```

---

## D. 리더기 정보 조회

### `reader.IsConnected`

현재 연결 여부를 즉시 반환하는 속성입니다.

```csharp
if (reader.IsConnected)
    Console.WriteLine("연결됨");
```

### `reader.PingAsync()`

리더기에 Version 요청을 보내 응답 여부를 확인합니다.  
연결 안 됨, 타임아웃, 예외 — 모두 예외 없이 `false`를 반환합니다.

```csharp
bool alive = await reader.PingAsync(timeoutMs: 500);
Console.WriteLine(alive ? "OK" : "응답 없음");

// DefaultTimeoutMs 사용 (timeoutMs 생략)
bool alive2 = await reader.PingAsync();
```

### `reader.GetReaderInfoAsync()`

Version 요청 결과를 `ReaderInfo` 구조체로 반환합니다.

```csharp
ReaderInfo info = await reader.GetReaderInfoAsync();
Console.WriteLine(info);
// 출력 예: [Serial] COM3 @ 115200 bps | FW: IS-3500K_V1.8 | Connected
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `FirmwareVersion` | `string` | 펌웨어 버전 문자열 |
| `Channel` | `ChannelType` | 연결 채널 (Serial / Socket) |
| `IsConnected` | `bool` | 현재 연결 여부 |
| `PortOrAddress` | `string?` | 포트 이름 또는 IP:Port |

---

## E. Raw 패킷 모니터링

`IksungReaderOptions.LogRawPackets = true`로 활성화하면  
모든 송신(TX)·수신(RX) 패킷의 raw 바이트를 이벤트로 받습니다.

```csharp
var options = new IksungReaderOptions { LogRawPackets = true };
await using var reader = await IksungReader.ConnectSerialAsync("COM3", 115200, options);

reader.RawPacketReceived += (sender, e) =>
{
    string dir = e.IsTransmit ? "TX" : "RX";
    string hex = BitConverter.ToString(e.Data).Replace("-", " ");
    Console.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] {dir}: {hex}");
};

// ReadVersion 실행 시 TX/RX 패킷이 출력됩니다
string version = await reader.ReadVersionAsync();
```

출력 예:
```
[14:30:01.123] TX: 01 00 10 00 00 10 03
[14:30:01.131] RX: 01 00 10 01 00 0F 49 53 2D 33 35 30 30 4B 5F 56 31 2E 38 FF 03
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `Data` | `byte[]` | on-wire raw 바이트 |
| `IsTransmit` | `bool` | `true`=TX, `false`=RX |
| `Timestamp` | `DateTime` | 캡처 시각 |

> **성능 주의**: 패킷마다 `byte[]` 복사본이 생성됩니다.  
> 고속 AutoRead 환경에서는 불필요할 때 비활성화하세요.

---

## F. 자동 재연결

`IksungReaderOptions.AutoReconnect = true`이면,  
케이블 뽑힘 등 **비정상 끊김** 감지 시 자동으로 재연결을 시도합니다.

- `ReconnectDelayMs`마다 재시도
- 재연결 성공 시 `ConnectionChanged(true)` 이벤트 발생
- `DisconnectAsync()`를 명시적으로 호출하면 자동 재연결 **중단**

```csharp
var options = new IksungReaderOptions
{
    AutoReconnect    = true,
    ReconnectDelayMs = 2000,   // 2초마다 재시도
};

await using var reader = await IksungReader.ConnectSerialAsync("COM3", 115200, options);

reader.ConnectionChanged += (_, connected) =>
    Console.WriteLine(connected ? "✓ 재연결됨!" : "✗ 연결 끊김 — 재시도 중");
```

> **AutoReconnect 적용 채널** — V1 현재 Serial 채널에서 검증되어 있습니다.
> PC/SC 채널은 OS 의 카드/리더 상태 변화에 따라 동작이 달라질 수 있으므로
> 끊김 감지 후 `ConnectPcscAsync` 를 직접 호출하는 패턴도 함께 고려하세요.

---

## G. 명시적 연결 해제

`reader.DisconnectAsync()`는 AutoReconnect가 켜져 있어도 재연결을 방지합니다.

```csharp
// 케이블 교체, 포트 변경 전 명시적 해제
await reader.DisconnectAsync();
Console.WriteLine("연결 해제됨 (AutoReconnect 중단)");

// await using 패턴에서는 DisposeAsync가 자동으로 해제
await using var reader = await IksungReader.ConnectSerialAsync("COM3");
// ... 블록 종료 시 자동 해제 + AutoReconnect 중단
```

---

## 전체 예제

```csharp
// 포트 자동 탐색 + 연결 + 모니터링
var ports = await IksungReaderDiscovery.ScanIksungPortsAsync();
if (ports.Count == 0) { Console.WriteLine("리더기 없음"); return; }

var options = new IksungReaderOptions
{
    AutoReconnect = true,
    LogRawPackets = true,
};

await using var reader = await IksungReader.ConnectSerialAsync(ports[0], 115200, options);

reader.ConnectionChanged += (_, ok) =>
    Console.WriteLine(ok ? "연결됨" : "끊김 — 재연결 중");

reader.RawPacketReceived += (_, e) =>
    Console.WriteLine($"{(e.IsTransmit ? "TX" : "RX")}: {BitConverter.ToString(e.Data)}");

ReaderInfo info = await reader.GetReaderInfoAsync();
Console.WriteLine(info);

bool ok2 = await reader.PingAsync();
Console.WriteLine($"Ping: {ok2}");
```

---

## 관련 샘플

| 샘플 | 경로 |
|------|------|
| NET8 | `NET8-Samples/14-SystemFeatures/` |
| NET4x | `NET4x-Samples/14-SystemFeatures/` |

```bash
# 빌드 및 실행 (NET8)
dotnet run --project NET8-Samples/14-SystemFeatures -- COM3
dotnet run --project NET8-Samples/14-SystemFeatures -- scan

# 빌드 및 실행 (NET4x)
dotnet build NET4x-Samples/14-SystemFeatures/SystemFeatures.Net4x.csproj
NET4x-Samples/14-SystemFeatures/bin/Debug/net472/SystemFeatures.Net4x.exe COM3
NET4x-Samples/14-SystemFeatures/bin/Debug/net472/SystemFeatures.Net4x.exe scan
```

---

[← 목차](README.md)
