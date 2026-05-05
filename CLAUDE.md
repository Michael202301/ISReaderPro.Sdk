# CLAUDE.md — ISReaderPro SDK

이 문서는 Claude Code 가 작업 시작 시 자동으로 읽는 프로젝트 가이드다.
모든 작업은 이 문서의 규칙을 준수해야 한다.

> **첫 작업 진입 시 반드시 같이 읽을 것:** [`docs/HANDOFF.md`](docs/HANDOFF.md)
> ↑ 본 SDK 의 설계 결정 / Phase 1 task 리스트 / V6.01 코드 추출 매핑.

---

## 프로젝트 개요

- **프로젝트명:** ISReaderPro SDK (NuGet 패키지명: `Iksung.Reader`)
- **목적:** 익성전자 NFC/RFID 리더기를 .NET 에서 쉽게 제어하는 무료 오픈소스 SDK
- **타깃 사용자:** 일반 .NET 개발자 (한국 내수 SI/제조업 다수)
- **지원 OS:** Windows + Linux + macOS (cross-platform)
- **하드웨어 범위:** **FTDI 칩 탑재 리더기 한정** (USB-to-Serial / D2XX)
- **라이선스:** MIT
- **개발 환경:** Visual Studio 2022 또는 VS Code, **.NET 8** (`net8.0`)
- **모태:** ISReaderPro V6.01 의 검증된 통신 코드를 추출/재정리

### 채널 우선순위 (대다수 시리얼 통신)

| 우선순위 | 채널 | 구현 클래스 (internal) | NuGet |
|----------|------|------------------------|-------|
| 1 | Serial (System.IO.Ports) | `SerialIksungChannel` | 핵심 |
| 2 | FTDI D2XX | `FtdiIksungChannel` | 핵심 |
| 3 | PC/SC (winscard / pcsc-lite) | `PcscIksungChannel` | 핵심 (lazy load) |
| 4 | TCP/IP Socket | `SocketIksungChannel` | 핵심 |

→ 단일 NuGet 패키지(`Iksung.Reader`) 에 4개 채널 모두 포함.

---

## 디렉토리 구조

```
ISReaderPro.Sdk/
├── CLAUDE.md                               # 본 문서 — 작업 시작 시 자동 로드
├── README.md                               # GitHub 첫 화면 (30초 Quickstart)
├── LICENSE.md                              # MIT
├── CHANGELOG.md                            # 버전별 변경 이력 (수동 관리)
├── .gitignore
│
├── docs/
│   ├── HANDOFF.md                          # ★ 첫 작업 시 반드시 읽을 것
│   ├── quickstart.md                       # 5분 입문
│   ├── platform-setup-windows.md
│   ├── platform-setup-linux.md
│   ├── platform-setup-mac.md
│   └── api-reference/                      # XML 주석 → DocFX 자동 생성
│
├── src/
│   └── Iksung.Reader/                      # 단일 프로젝트 → Iksung.Reader.dll
│       ├── Iksung.Reader.csproj
│       │
│       ├── IksungReader.cs                 # public sealed class — 고객 진입점
│       ├── Models/                         # public records
│       │   ├── ChannelType.cs
│       │   ├── CardInfo.cs
│       │   ├── ReaderInfos.cs              # SerialReaderInfo / FtdiReaderInfo / ...
│       │   ├── AutoReadOptions.cs
│       │   └── EventArgs.cs
│       ├── Exceptions/                     # public
│       │   ├── IksungException.cs
│       │   ├── ChannelDisconnectedException.cs
│       │   ├── IksungTimeoutException.cs
│       │   └── IksungProtocolException.cs
│       │
│       └── Internals/                      # 모두 internal — 고객 안 보임
│           ├── IIksungChannel.cs
│           ├── Channels/
│           │   ├── SerialIksungChannel.cs
│           │   ├── FtdiIksungChannel.cs
│           │   ├── PcscIksungChannel.cs
│           │   └── SocketIksungChannel.cs
│           ├── Protocol/
│           │   ├── Constants.cs
│           │   ├── PacketBuilder.cs
│           │   └── PacketParser.cs
│           ├── Native/
│           │   ├── FtdiNative.cs           # DllImport "ftd2xx" (자동 .dll/.so/.dylib)
│           │   └── WinscardNative.cs       # DllImport "winscard" (Windows)
│           └── AssemblyInfo.cs
│               # [assembly: InternalsVisibleTo("Iksung.Reader.Tests")]
│
├── samples/
│   ├── HelloWorld.Console/                 # 5줄짜리 UID 읽기
│   ├── Wpf.Sample/
│   ├── WinForms.Sample/
│   ├── AspNetCore.WebApi/                  # REST API 래퍼 예제
│   └── Linux.Console/
│
├── tests/
│   └── Iksung.Reader.Tests/                # InternalsVisibleTo 로 internal 접근
│
├── ISReaderPro.Sdk.sln
└── .github/workflows/
    ├── build-test.yml                      # Win/Linux/Mac matrix CI
    └── publish-nuget.yml                   # v태그 push → NuGet 자동 배포
```

---

## CRITICAL 규칙 (절대 위반 금지)

### 🚫 public / internal 분리 정책 위반 금지

**고객 IntelliSense 에 노출되는 public 타입:**
- `IksungReader` (sealed class) + 정적 팩토리만 진입점
- public records: `CardInfo`, `SerialReaderInfo`, `FtdiReaderInfo`, `PcscReaderInfo`
- public enums: `ChannelType`, `CardType`, `ReaderInterface` (Flags)
- public 옵션: `AutoReadOptions`
- public eventargs: `TagArrivedEventArgs`, `RawDataReceivedEventArgs`
- public 예외: `IksungException` 및 그 sealed 파생

**모두 internal 처리:**
- `IIksungChannel` 인터페이스
- 4개 채널 구현체 (`SerialIksungChannel`, `FtdiIksungChannel`, `PcscIksungChannel`, `SocketIksungChannel`)
- 모든 protocol 빌더 / parser / 상수
- 모든 P/Invoke (`FtdiNative`, `WinscardNative`)

**이유:** 고객이 IntelliSense 에서 잘못된 타입 선택 못 하도록. v1 에서는 internal 유지, v2 에서 고객 피드백 받고 선택적으로 public 승격 검토.

### 🚫 public API 의 비동기/예외/Cancellation 패턴 위반 금지

- 모든 I/O 메서드는 `async Task` / `async Task<T>` (`async void` 금지)
- 모든 I/O 메서드는 마지막 인자로 `CancellationToken ct = default`
- 실패는 **예외 throw** (`IksungException` 계열). bool 반환 금지
- 동기 wrapper 제공 안 함 (`Result` 같은 .GetAwaiter().GetResult() 권장 안함 — 데드락 유발)
- `Try*` 변형은 v1 에서 제공 안 함 (필요 시 v2 검토)

### 🚫 IksungReader 외부 직접 인스턴스화 금지

```csharp
public sealed class IksungReader : IAsyncDisposable
{
    private readonly IIksungChannel _channel;

    // private 생성자 — new IksungReader(...) 외부에서 불가
    private IksungReader(IIksungChannel channel, ChannelType type) { ... }

    public static Task<IksungReader> ConnectSerialAsync(...) { ... }
    public static Task<IksungReader> ConnectFtdiAsync(...)   { ... }
    // ...
}
```

### 🚫 V6.01 / WPF 종속성 유입 금지

V6.01 코드를 추출할 때 **반드시 제거할 것**:
- `System.Windows.Application.Current.Dispatcher` — UI 스레드 마샬링 코드
- `ObservableCollection<T>` (UI 바인딩용) — `IReadOnlyList<T>` 또는 `event` 로 전환
- `[ObservableProperty]` (CommunityToolkit.Mvvm) — SDK 는 ViewModel 아님
- `MessageDialog.Show*` 호출

**SDK 는 UI/MVVM 종속성 0** — `dotnet add package CommunityToolkit.Mvvm` 같은 건 절대 추가하지 말 것.

### 🚫 매직 상수 인라인 사용 금지

V6.01 의 `Helpers/ProtocolConstants.cs` 를 그대로 추출 → SDK 의 `Internals/Protocol/Constants.cs` 로 이동. 새 코드에서도 모든 byte 값은 명명 상수로 참조.

### 🚫 외부 NuGet 의존성 추가 금지 (예외 있음)

본 SDK 는 BCL (System.IO.Ports 포함) 외 **외부 의존성 0** 을 목표로 한다. 다음 외에는 추가 금지:

- `System.IO.Ports` (BCL 일부지만 별도 NuGet)
- (테스트 프로젝트만) `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Moq`

새 패키지 추가 필요 시 반드시 사용자 확인.

### 🚫 동기 P/Invoke 차단 호출 금지

`FT_Read`, `SCardTransmit` 같은 P/Invoke 동기 호출은 짧게(<100ms) 끝나는 경우만 직접 호출. 그 외는 `Task.Run` 또는 자체 비동기 wrapping 으로 UI 스레드 차단 방지.

---

## 코딩 규칙

### 명명 규칙
- 클래스/메서드/속성: `PascalCase`
- 매개변수/지역변수: `camelCase`
- 인터페이스: `I` 접두사 (`IIksungChannel`)
- private 필드: `_camelCase`
- 상수: `PascalCase` (.NET 컨벤션)
- public records: 위치 매개변수 또는 init-only properties

### 파일 배치
- 하나의 클래스/인터페이스 = 하나의 파일 (파일명 = 타입명)
- public 은 `src/Iksung.Reader/` 또는 `Models/`, `Exceptions/`
- internal 은 모두 `src/Iksung.Reader/Internals/` 하위
- `Internals` 디렉토리 자체가 namespace 분리 신호

### Nullable 참조 타입
- `<Nullable>enable</Nullable>` 활성화 (csproj)
- nullable 가능 참조는 `?` 명시
- public API 반환값은 가급적 non-null

### 비동기 패턴
- 메서드 이름에 `Async` 접미사
- 모든 비동기 메서드는 `CancellationToken` 파라미터
- `ConfigureAwait(false)` 는 SDK 내부에서 100% 사용 (UI/싱크 컨텍스트 회피)

### Cross-platform
- `[DllImport("ftd2xx")]` 처럼 확장자 생략 → .NET 이 OS별 .dll/.so/.dylib 자동 매핑
- 경로 상수는 `Path.Combine` 사용
- Linux/Mac 의존 코드는 `RuntimeInformation.IsOSPlatform(...)` 으로 가드

### XML 주석 의무 (public API 모두)
```csharp
/// <summary>
/// IS-3500K 등 시리얼 인터페이스 리더기에 비동기 연결한다.
/// </summary>
/// <param name="portName">시리얼 포트 이름 (Windows: "COM3", Linux: "/dev/ttyUSB0")</param>
/// <param name="baudRate">통신 속도 (보통 115200)</param>
/// <param name="ct">취소 토큰</param>
/// <returns>연결된 IksungReader 인스턴스. 사용 종료 시 DisposeAsync 호출.</returns>
/// <exception cref="ChannelDisconnectedException">포트 열기 실패</exception>
public static Task<IksungReader> ConnectSerialAsync(string portName, int baudRate, CancellationToken ct = default);
```

---

## 빌드 / 검증 명령어

### 솔루션 빌드
```bash
dotnet build ISReaderPro.Sdk.sln
```

### Release + 모든 OS 검증 (CI 기본)
```bash
dotnet build ISReaderPro.Sdk.sln -c Release /warnaserror
```

### 테스트 실행
```bash
dotnet test
```

### NuGet 패키징 (v태그 push 로 자동 트리거)
```bash
dotnet pack src/Iksung.Reader/Iksung.Reader.csproj -c Release -o ./nupkg
```

### Acceptance Criteria 표준
모든 step 의 AC 는 다음 중 최소 하나를 포함해야 한다:
- 솔루션 빌드 성공 (`dotnet build`)
- 컴파일 경고 0 (`/warnaserror`)
- 단위 테스트 통과 (있는 경우)
- HelloWorld.Console 샘플 빌드 성공

**실제 하드웨어 연결 검증은 사람이 수행한다.** AC 에 "보드와 연결 후 동작 확인" 같은 항목 금지.

---

## V6.01 ↔ SDK 자산 매핑 (요약)

상세는 [`docs/HANDOFF.md`](docs/HANDOFF.md) 참조.

| V6.01 (`D:\Work\IS-ReaderPro-Software\ISReaderPro-V6.01\ISReaderPro\`) | SDK 신규 위치 | 변환 필요 |
|---|---|---|
| `Services/IRawIoChannel.cs` | `Internals/IIksungChannel.cs` | 이름만 변경 + async 화 |
| `Services/SerialService.cs` (raw I/O 부분) | `Internals/Channels/SerialIksungChannel.cs` | UI/Dispatcher 제거, async 화 |
| `Services/SocketService.cs` (raw I/O 부분) | `Internals/Channels/SocketIksungChannel.cs` | UI/Dispatcher 제거 |
| `Services/PcscService.cs` (raw I/O + STX/ETX↔APDU 변환) | `Internals/Channels/PcscIksungChannel.cs` | 그대로 추출 (이미 검증됨) |
| `Helpers/ProtocolConstants.cs` | `Internals/Protocol/Constants.cs` | 그대로 |
| `Helpers/ProtocolHelper.cs` | `Internals/Protocol/PacketBuilder.cs` | 그대로 |
| `Helpers/BleAuthenticationHelper.cs` | (선택) `Internals/Auth/AuthHelper.cs` | namespace 만 변경 |
| `Helpers/Crc32.cs` | `Internals/Crypto/Crc32.cs` | 그대로 |

---

## Claude Code 작업 가이드

### 작업 전 반드시 확인할 것
1. 본 CLAUDE.md 의 CRITICAL 규칙 위반 여부
2. `docs/HANDOFF.md` 의 Phase 별 task 진행 상황
3. public / internal 분리 정책 준수 여부
4. V6.01 추출 시 WPF/UI 종속성 완전 제거 여부

### 새 public 타입 추가 시 체크리스트
- [ ] `Iksung.Reader.dll` 의 public surface 에 정말 필요한가?
- [ ] internal 로도 가능하지 않은가?
- [ ] XML 주석 작성했는가?
- [ ] 단위 테스트가 있는가?
- [ ] README / Quickstart 의 30초 예제와 충돌 없는가?

### 새 internal 채널 추가 시 체크리스트
- [ ] `IIksungChannel` 구현?
- [ ] `IAsyncDisposable` 구현?
- [ ] Cross-platform 동작 확인 (DllImport 이름 검증)?
- [ ] `ConnectAsync` / `SendAndReceiveAsync` 가 `CancellationToken` 정확히 처리?
- [ ] 단위 테스트 (Mock 또는 통합) 있는가?

### 모호하면 멈춰라
- 새 NuGet 패키지 추가 필요 → 반드시 사용자 확인
- public API 시그니처 변경 → 반드시 사용자 확인
- 4개 채널 외 새 채널 추가 (예: BLE Classic) → 반드시 사용자 확인
- `Try*` 메서드 추가 → 반드시 사용자 확인 (v1 정책상 불추가)
- `IIksungChannel` 을 public 으로 승격 → 반드시 사용자 확인 (v1 정책상 internal 유지)

---

## 미확정 항목 (TBD)

다음 항목들은 v1.0 출시 후 고객 피드백 보고 결정한다.

- [ ] DI 등록 헬퍼 (`services.AddIksungReader()`) — `Microsoft.Extensions.DependencyInjection.Abstractions` 의존성 추가 가치 있는가?
- [ ] 로깅 (`Microsoft.Extensions.Logging.Abstractions`) 도입 — 사용자 디버깅 편의 vs 의존성 추가
- [ ] `IIksungChannel` public 승격 (custom 채널 주입 허용)
- [ ] Bootloader (펌웨어 업데이트) 헬퍼 — V6.01 의 `Services/Bootloader/` 추출
- [ ] Mifare Classic / DESFire 고수준 헬퍼
- [ ] Java SDK / Python SDK (필요성 확정 후)

---

## 펌웨어와의 관계

본 SDK 는 IS-3500K 펌웨어와 통신한다. 통신 프로토콜은 펌웨어 매뉴얼이 단일 진실 공급원(SSOT) 이다:

```
D:\Work\IS-3500\FW\IS-3500K-zephyr-V1.8\Manual\
```

V6.01 의 `Manual\*.md` (각 탭별 프로토콜 문서) 도 참조 가치가 높다.

**프로토콜 변경 시 절차:**
1. 펌웨어 매뉴얼 먼저 확인
2. V6.01 의 `Helpers/ProtocolConstants.cs` 동기화 여부 확인
3. SDK 의 `Internals/Protocol/Constants.cs` 갱신
4. 단위 테스트 추가 / 갱신
