# Handoff — ISReaderPro SDK Phase 1 Kickoff

> **본 문서의 목적:** 새 Claude Code 세션이 ISReaderPro V6.01 작업의 모든 결정사항을
> 이어받아 SDK Phase 1 을 즉시 시작할 수 있게 한다.
>
> **첫 세션 첫 프롬프트 권장:**
> > `D:\Work\IS-ReaderPro-Software\ISReaderPro.Sdk\` 에서 작업 시작.
> > `CLAUDE.md` 와 `docs/HANDOFF.md` 읽고 Phase 1 부터 진행해주세요.

---

## 1. 본 SDK 의 배경 (한 줄 요약)

> 익성전자가 기존에 제공하던 FTDI D2XX 기반 SDK (`IS_D2XX.cs`) 가 C-스타일 API 라
> .NET 고객이 불편해 함. ISReaderPro V6.01 의 검증된 통신 코드를 추출/재정리하여
> **무료 오픈소스 NuGet 패키지** 로 재출시.

---

## 2. 사용자가 명시한 요구사항 (확정)

| 항목 | 결정 |
|------|------|
| 타깃 사용자 | **일반 .NET 개발자** (한국 내수 SI/제조업 다수) |
| 지원 OS | **Windows + Linux + macOS** (Linux/Mac 은 ISReaderPro Linux/Mac 포팅 후 단계적) |
| 라이선스 | **MIT** (free, 상업 이용 가능) |
| 채널 우선순위 | **Serial 1순위 → FTDI D2XX 2순위 → PCSC 3순위 → Socket 4순위** |
| 하드웨어 범위 | **FTDI 칩 탑재 리더기 한정** (USB-to-Serial 또는 D2XX) |
| 인터페이스/구현체 노출 | **모두 internal** — 고객 IntelliSense 에 안 보이게 |
| 패키지 구조 | **단일 NuGet 패키지** (`Iksung.Reader`) — 분리 안 함 |

---

## 3. 설계 결정 (옵션 B 선택 + internal 정책)

### 3.1 채널 추상화 + 단일 SDK (옵션 B)

```
┌─────────────────────────────────────────────────────────┐
│      IksungReader (public sealed) ← 고객 진입점          │
│   ─────────────────────────────────────────────         │
│   ConnectSerialAsync() / ConnectFtdiAsync() / ...       │
│   ReadVersionAsync() / ReadIso14443aUidAsync() / ...    │
│   StartAutoReadAsync() / event TagArrived               │
│   SendRawCommandAsync() ← 전문가용 raw 명령              │
└──────────────┬──────────────────────────────────────────┘
               │ uses (internal)
               ▼
┌─────────────────────────────────────────────────────────┐
│         IIksungChannel (internal interface)              │
│   bool IsConnected                                       │
│   Task ConnectAsync(CancellationToken)                   │
│   Task<byte[]> SendAndReceiveAsync(packet, timeout, ct)  │
│   event Action<byte[]>? RawReceived                      │
└────┬─────────┬──────────┬──────────┬────────────────────┘
     │         │          │          │
     ▼         ▼          ▼          ▼
SerialChan  FtdiChan   PcscChan   SocketChan   (모두 internal)
```

### 3.2 Public / Internal 분리 정책

**고객 IntelliSense 에 노출되는 public 타입 (총 14개 추정):**
- `IksungReader` (sealed class) + 정적 팩토리만 진입점
- public records: `CardInfo`, `SerialReaderInfo`, `FtdiReaderInfo`, `PcscReaderInfo`
- public enums: `ChannelType`, `CardType`, `ReaderInterface` (Flags)
- public 옵션: `AutoReadOptions`
- public eventargs: `TagArrivedEventArgs`, `RawDataReceivedEventArgs`
- public 예외: `IksungException` 및 그 sealed 파생 (3종)

**모두 internal 처리:**
- `IIksungChannel` 인터페이스
- 4개 채널 구현체
- 모든 protocol 빌더 / parser / 상수
- 모든 P/Invoke (`FtdiNative`, `WinscardNative`)

### 3.3 DLL 구성

- 단일 어셈블리: **`Iksung.Reader.dll`**
- 단일 NuGet 패키지: **`Iksung.Reader`**
- 외부 의존성: 0 (단, `System.IO.Ports` 는 BCL 일부지만 별도 패키지)
- 고객 설치: `dotnet add package Iksung.Reader`

---

## 4. Phase 1 Task 리스트 (1주 목표)

### Task 1.1 — 솔루션 / 프로젝트 골격 생성 (반나절)

**생성할 csproj:**

| Project | TargetFramework | OutputType | 설명 |
|---------|-----------------|------------|------|
| `src/Iksung.Reader/Iksung.Reader.csproj` | `net8.0` | Library | 메인 SDK |
| `samples/HelloWorld.Console/HelloWorld.Console.csproj` | `net8.0` | Exe | 5줄짜리 검증 |
| `tests/Iksung.Reader.Tests/Iksung.Reader.Tests.csproj` | `net8.0` | Library + xunit | 단위 테스트 |

**csproj 핵심 설정:**
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <PackageId>Iksung.Reader</PackageId>
  <Authors>iksung-electronics</Authors>
  <Description>Easy-to-use SDK for IKSUNG NFC/RFID readers. Cross-platform.</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/iksung-electronics/iksung-reader-sdk</PackageProjectUrl>
  <RepositoryUrl>https://github.com/iksung-electronics/iksung-reader-sdk</RepositoryUrl>
</PropertyGroup>
```

**ItemGroup (메인 프로젝트):**
```xml
<ItemGroup>
  <PackageReference Include="System.IO.Ports" Version="8.0.0" />
</ItemGroup>

<ItemGroup>
  <InternalsVisibleTo Include="Iksung.Reader.Tests" />
</ItemGroup>
```

**ItemGroup (테스트 프로젝트):**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.x" />
  <PackageReference Include="xunit" Version="2.x" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.x" />
  <PackageReference Include="Moq" Version="4.x" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="..\..\src\Iksung.Reader\Iksung.Reader.csproj" />
</ItemGroup>
```

**솔루션 파일:**
```bash
dotnet new sln -n ISReaderPro.Sdk
dotnet sln add src/Iksung.Reader/Iksung.Reader.csproj
dotnet sln add samples/HelloWorld.Console/HelloWorld.Console.csproj
dotnet sln add tests/Iksung.Reader.Tests/Iksung.Reader.Tests.csproj
```

**완료 조건 (AC):**
- [ ] `dotnet build ISReaderPro.Sdk.sln` 성공
- [ ] 경고 0 (`/warnaserror`)
- [ ] 빈 클래스라도 빌드 OK

---

### Task 1.2 — V6.01 자산 추출 (반나절)

**원본 경로:** `D:\Work\IS-ReaderPro-Software\ISReaderPro-V6.01\ISReaderPro\`

#### A. 그대로 복사 (namespace만 변경)

| V6.01 원본 | SDK 신규 위치 | 변환 |
|-----------|---------------|------|
| `Helpers/ProtocolConstants.cs` | `src/Iksung.Reader/Internals/Protocol/Constants.cs` | namespace `ISReaderPro.Sdk.Internals.Protocol` + `internal static class` |
| `Helpers/ProtocolHelper.cs` | `src/Iksung.Reader/Internals/Protocol/PacketBuilder.cs` | namespace 변경 + `internal static class PacketBuilder` (이름 변경) |
| `Helpers/Crc32.cs` | `src/Iksung.Reader/Internals/Crypto/Crc32.cs` | namespace + internal |
| `Services/IRawIoChannel.cs` | `src/Iksung.Reader/Internals/IIksungChannel.cs` | 인터페이스 이름 변경 + async 화 |

#### B. 일부 추출 (UI/Dispatcher 종속성 제거)

| V6.01 원본 | SDK 신규 위치 | 제거할 것 |
|-----------|---------------|-----------|
| `Services/SerialService.cs` | `src/Iksung.Reader/Internals/Channels/SerialIksungChannel.cs` | `Application.Current.Dispatcher.Invoke` / `ObservableCollection` / `PacketReceived` UI event |
| `Services/SocketService.cs` | `src/Iksung.Reader/Internals/Channels/SocketIksungChannel.cs` | 동일 |
| `Services/PcscService.cs` | `src/Iksung.Reader/Internals/Channels/PcscIksungChannel.cs` | 동일. 단, **STX/ETX↔APDU hybrid 변환** 코드는 그대로 유지 |

#### C. 추출 보류 (Phase 2 이후)

- `Helpers/BleAuthenticationHelper.cs` — Phase 3 (고수준 API) 시점에 추출
- `Services/Bootloader/*.cs` — Phase 4 (확장) 시점에 추출
- `Helpers/SerialPacketEventArgs.cs` — internal 이벤트로 재설계

#### 추출 시 공통 변환 규칙

1. namespace: `ISReaderPro.Services` → `ISReaderPro.Sdk.Internals` (또는 하위)
2. 접근 한정자: `public class` → `internal class`
3. Dispatcher 호출 제거 → 그대로 직접 호출 (호출 측에서 ConfigureAwait(false) 사용)
4. ObservableCollection → IReadOnlyList + 변경 이벤트
5. WPF 의존 namespace using 모두 제거 (`System.Windows`, `CommunityToolkit.Mvvm`)
6. `Send(byte[])` → `Task<byte[]> SendAndReceiveAsync(byte[], int, CancellationToken)` 비동기화

**완료 조건 (AC):**
- [ ] `Constants.cs` / `PacketBuilder.cs` 추출 완료, internal 컴파일 OK
- [ ] `IIksungChannel.cs` 인터페이스 정의 완료 (async)
- [ ] `SerialIksungChannel.cs` 추출 + UI 종속성 0 검증

---

### Task 1.3 — Public Facade 작성 (`IksungReader`) (반나절)

**파일:** `src/Iksung.Reader/IksungReader.cs`

```csharp
using System.Text;
using ISReaderPro.Sdk.Internals;
using ISReaderPro.Sdk.Internals.Channels;
using ISReaderPro.Sdk.Internals.Protocol;

namespace ISReaderPro.Sdk;

/// <summary>
/// IKSUNG NFC/RFID 리더기 SDK 의 진입점.
/// </summary>
public sealed class IksungReader : IAsyncDisposable
{
    private readonly IIksungChannel _channel;
    private readonly ChannelType    _channelType;
    private bool _disposed;

    private IksungReader(IIksungChannel channel, ChannelType type)
    {
        _channel     = channel;
        _channelType = type;
    }

    public bool        IsConnected  => _channel.IsConnected;
    public ChannelType ConnectedVia => _channelType;

    /// <summary>시리얼 포트로 리더기에 연결.</summary>
    public static async Task<IksungReader> ConnectSerialAsync(
        string portName, int baudRate, CancellationToken ct = default)
    {
        var channel = new SerialIksungChannel(portName, baudRate);
        await channel.ConnectAsync(ct).ConfigureAwait(false);
        return new IksungReader(channel, ChannelType.Serial);
    }

    // 다른 ConnectXxxAsync 들도 동일 패턴 (Phase 1 끝에서는 Serial 만 우선 구현)

    /// <summary>펌웨어 버전 문자열 조회.</summary>
    public async Task<string> ReadVersionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] packet = PacketBuilder.BuildPacket(
            ProtocolConstants.MAJOR_COMMON,
            ProtocolConstants.COMMON_VERSION);
        byte[] resp = await _channel.SendAndReceiveAsync(packet, 1000, ct).ConfigureAwait(false);
        return PacketParser.ExtractDataAsAscii(resp);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _channel.DisposeAsync().ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IksungReader));
    }
}
```

**완료 조건 (AC):**
- [ ] `IksungReader.ConnectSerialAsync()` + `ReadVersionAsync()` + `DisposeAsync()` 구현
- [ ] 빌드 + 경고 0
- [ ] 단위 테스트: Mock channel 로 ReadVersionAsync 검증

---

### Task 1.4 — Public Models / Exceptions (1시간)

**`src/Iksung.Reader/Models/ChannelType.cs`:**
```csharp
namespace ISReaderPro.Sdk;

public enum ChannelType { Serial, Ftdi, Pcsc, Socket }
```

**`src/Iksung.Reader/Exceptions/IksungException.cs`:**
```csharp
namespace ISReaderPro.Sdk;

public class IksungException : Exception
{
    public IksungException(string message) : base(message) { }
    public IksungException(string message, Exception innerException) : base(message, innerException) { }
}

public sealed class ChannelDisconnectedException : IksungException
{
    public ChannelDisconnectedException(string message) : base(message) { }
}

public sealed class IksungTimeoutException : IksungException
{
    public IksungTimeoutException(string message) : base(message) { }
}

public sealed class IksungProtocolException : IksungException
{
    public IksungProtocolException(string message) : base(message) { }
}
```

**완료 조건 (AC):**
- [ ] public 타입 IntelliSense 노출 검증 (테스트 프로젝트에서 `using ISReaderPro.Sdk;` 후 확인)

---

### Task 1.5 — HelloWorld 콘솔 샘플 + README (1시간)

**`samples/HelloWorld.Console/Program.cs`:**
```csharp
using ISReaderPro.Sdk;

if (args.Length < 2)
{
    Console.WriteLine("Usage: HelloWorld.Console <portName> <baudRate>");
    Console.WriteLine("  Windows: HelloWorld.Console COM3 115200");
    Console.WriteLine("  Linux:   HelloWorld.Console /dev/ttyUSB0 115200");
    Console.WriteLine("  Mac:     HelloWorld.Console /dev/tty.usbserial-XXXX 115200");
    return;
}

string portName = args[0];
int    baudRate = int.Parse(args[1]);

await using var reader = await IksungReader.ConnectSerialAsync(portName, baudRate);
Console.WriteLine($"Connected via: {reader.ConnectedVia}");
Console.WriteLine($"Firmware version: {await reader.ReadVersionAsync()}");
```

**`README.md` 업데이트** — 30초 Quickstart 포함 (CLAUDE.md 의 가이드 참조).

**완료 조건 (AC):**
- [ ] `dotnet run --project samples/HelloWorld.Console -- COM3 115200` 빌드 성공 (실행은 사람이 검증)
- [ ] README 의 "Hello World" 코드 블록 4줄로 작동 가능 명시

---

## 5. Phase 별 큰 그림 (요약)

| Phase | 기간 | 마일스톤 |
|-------|------|---------|
| **Phase 1 (현재)** | 1주 | Serial 채널 동작 + HelloWorld + 첫 commit |
| Phase 2 | 1주 | FTDI 채널 추가 + Linux/Mac CI 검증 + `v0.1.0-preview` NuGet |
| Phase 3 | 2주 | 고수준 API (`ReadAnyCardAsync` / `StartAutoReadAsync`) + WPF 샘플 + `v1.0.0` 정식 NuGet |
| Phase 4 (선택) | 1~2주 | PCSC + Socket 채널 + REST API 샘플 + Bootloader 헬퍼 |

→ **6주 안에 v1.0 NuGet 정식 출시** 목표.

---

## 6. 참고 자산 위치

### V6.01 (모태 프로젝트)
```
D:\Work\IS-ReaderPro-Software\ISReaderPro-V6.01\
├── ISReaderPro\
│   ├── Helpers\ProtocolConstants.cs           ← 그대로 추출
│   ├── Helpers\ProtocolHelper.cs              ← 그대로 추출 (이름만 변경)
│   ├── Helpers\Crc32.cs                       ← 그대로 추출
│   ├── Helpers\BleAuthenticationHelper.cs     ← Phase 3 추출
│   └── Services\
│       ├── IRawIoChannel.cs                   ← 인터페이스 베이스
│       ├── SerialService.cs                   ← Serial 채널 베이스
│       ├── SocketService.cs                   ← Socket 채널 베이스
│       └── PcscService.cs                     ← PC/SC 채널 베이스 (hybrid APDU 구현 완료)
└── Manual\                                    ← 프로토콜 매뉴얼 13종 (참고 가치 높음)
```

### 기존 IKSUNG SDK (참고 / 호환성 검토용)
```
D:\Work\IS-ReaderPro-Software\FTDI-SDK\
├── DLL\IS_D2XX_NET\IS_D2XX.cs                ← 기존 .NET 래퍼 (개선 대상)
└── DLL_2015_64Bit\IS_DXX_NET_V1.0\           ← 64bit 변형
```

### 펌웨어 매뉴얼 (SSOT)
```
D:\Work\IS-3500\FW\IS-3500K-zephyr-V1.8\Manual\
└── IS-3500K_CCID_AutoRead_01_Common.md        ← 프로토콜 진실 소스
```

### Springcard 참고 (좋은 SDK 패턴 예시)
- https://github.com/springcard/springcard.pcsc.sdk

---

## 7. 새 세션 첫 메시지 템플릿

```
D:\Work\IS-ReaderPro-Software\ISReaderPro.Sdk\ 에서 작업 시작.
CLAUDE.md 와 docs/HANDOFF.md 읽고 Phase 1 Task 1.1 부터 진행해주세요.

확인 후 다음 순서로 진행:
1. 솔루션 / 프로젝트 골격 생성 (Task 1.1)
2. V6.01 자산 추출 — 우선 Constants/PacketBuilder/IIksungChannel/SerialIksungChannel (Task 1.2)
3. IksungReader public facade + Serial.ConnectAsync + ReadVersionAsync (Task 1.3)
4. Public Models / Exceptions (Task 1.4)
5. HelloWorld 콘솔 + README 갱신 (Task 1.5)

각 Task 끝나면 dotnet build /warnaserror 통과 + 사용자 보고.
```

---

## 8. Definition of Done (Phase 1)

다음 모두 만족 시 Phase 1 완료:

- [ ] `dotnet build ISReaderPro.Sdk.sln` 경고 0, 오류 0
- [ ] `Iksung.Reader.dll` 의 public API 가 IntelliSense 에 14개 이하 타입만 노출
- [ ] `IksungReader.ConnectSerialAsync()` 로 V6.01 의 SerialService 동등 동작 (포트 열기/패킷 송수신)
- [ ] `IksungReader.ReadVersionAsync()` 로 펌웨어 버전 ASCII 문자열 반환
- [ ] HelloWorld.Console 빌드 성공 (실기 검증은 사람)
- [ ] 단위 테스트 최소 3개 (PacketBuilder / IksungReader.ReadVersionAsync mock / Constants 검증)
- [ ] CHANGELOG.md 의 `[Unreleased]` 섹션에 진행사항 기록

---

## 9. v1.0 출시 체크리스트 (Phase 3 끝 시점)

Phase 1 완료 후 v1.0 까지 가는 길:

- [ ] 4개 채널 모두 (Serial / FTDI / PCSC / Socket) 동작
- [ ] Linux / Mac CI 빌드 통과
- [ ] 고수준 API (`ReadAnyCardAsync` / `StartAutoReadAsync`) 동작
- [ ] WPF / WinForms / Console 샘플 3종
- [ ] DocFX API 레퍼런스 자동 생성
- [ ] README 의 30초 Quickstart 검증
- [ ] GitHub repo public + LICENSE.md 명시
- [ ] NuGet `v1.0.0` 배포 + 릴리스 노트
