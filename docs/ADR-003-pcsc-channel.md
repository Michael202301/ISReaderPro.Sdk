# ADR-003: PC/SC (CCID) 채널 지원 추가

- **날짜**: 2026-05-10
- **상태**: Accepted
- **결정자**: SDK 팀

---

## 맥락 (Context)

Iksung.Reader SDK 는 현재 Serial(UART)과 TCP/IP Socket 두 채널만 지원한다.
IS-NFC 시리즈 리더기 중 USB CCID 모드로 동작하는 제품(예: IS-3500Z, IS-3500K)은
Windows PC/SC 스택을 통해 연결되므로 `IIksungChannel` 구현이 없으면 SDK 로 제어할 수 없다.

고객사 요청 사항:

- USB 케이블만으로 리더기에 연결하여 카드 읽기 / 설정 변경 / 펌웨어 업데이트를 수행
- Serial 드라이버 설치 없이 USB HID/CCID 드라이버(OS 내장)만으로 동작

---

## 결정 (Decision)

`winscard.dll` 을 직접 P/Invoke 하는 `PcscIksungChannel` 을 구현한다.

- ISReaderPro V6.01 `PcscService.cs` 를 참조 구현으로 포팅
- IS-3500K IS-NFC 시리즈 장비에서 V6.05 기준으로 검증 완료된 로직 재사용
- 추가 NuGet 패키지 의존성 없음

공개 API 추가:

| 추가 항목 | 위치 |
|-----------|------|
| `PcscIksungChannel` | `Internals/Channels/PcscIksungChannel.cs` |
| `IksungPcscDiscovery` | `IksungPcscDiscovery.cs` |
| `IksungReader.ConnectPcscAsync()` | `IksungReader.cs` |

---

## 근거 (Rationale)

### 왜 P/Invoke 직접 구현인가?

| 방법 | 장점 | 단점 |
|------|------|------|
| `winscard.dll` 직접 P/Invoke | 의존성 0, 검증된 코드 재사용 가능 | Windows 전용 |
| `PCSC` NuGet 패키지 | 크로스플랫폼(macOS, Linux pcscd) | 추가 패키지 의존성, ADR-001 정책과 충돌 가능성 |
| `PCSC.Iso7816` NuGet 패키지 | APDU 빌드 편의 | 과도한 추상화, 학습 비용 |

현재 대상 플랫폼이 Windows 전용(WinForms/WPF 앱과 함께 동작)이므로 P/Invoke가 가장 적합하다.

### STX/ETX ↔ APDU 변환

IS-3500K 펌웨어는 CCID 경로에서도 동일한 명령 바이트(CMD1/CMD2)를 사용하되,
ISO 7816-4 APDU 감싸기(CLA=FF, INS=CMD1, P1=CMD2, P2=00, Lc/Le)를 요구한다.
SDK 내부에서 자동 변환하므로 상위 레이어 코드 변경 없이 채널만 교체하면 된다.

---

## 구현 상세 (Implementation)

### PcscIksungChannel

- `IIksungChannel` 구현
- `ConnectAsync`: `SCardEstablishContext` → `SCardConnect(T=0|T=1 자동)` → ATR 캐싱
- `SendAndReceiveAsync`: STX 패킷 파싱 → APDU 빌드(4-Case) → `SCardTransmit` → SW 검증 → Data 반환
- `BeginRawIo` / `WriteRaw` / `ReadRaw`: 부트로더 펌웨어 업데이트(V6.0/V7.0) 지원
- Reader 모니터: 1초 polling 으로 리더 연결 끊김 감지 후 `ConnectionChanged` 이벤트 발사
- `SetBaudRate`: PC/SC 는 baud 개념 없음, no-op

### IksungPcscDiscovery

- `GetAvailableReaders()`: `SCardListReaders` 래퍼 — Smart Card Service 중지 시 빈 목록
- `StartMonitor()` / `StopMonitor()`: 백그라운드 polling 으로 Hot-plug 감지, `ReaderListChanged` 이벤트

### ConnectPcscAsync 팩토리

```csharp
await using var reader = await IksungReader.ConnectPcscAsync("iksung IS-3500Z 0");
```

---

## 제약사항 (Limitations)

- **Windows 전용**: `winscard.dll` 은 Windows Smart Card 서비스에 의존한다.
  비-Windows 환경(Linux, macOS)에서는 `PlatformNotSupportedException` 이 발생한다.
- **AutoRead push 미지원 (V1)**: PC/SC는 request-response 모델로만 동작하므로
  리더기가 자발적으로 패킷을 푸시하는 AutoRead 모드는 폴링 방식으로 대체해야 한다.
  V2에서 `SCardGetStatusChange` 이벤트 모델 기반 AutoRead 개선을 고려한다.
- **Smart Card Service 필요**: Windows 서비스 `SCardSvr` 가 실행 중이어야 한다.
  기본적으로 활성화되어 있으나, 일부 임베디드 Windows 환경에서는 비활성화 상태일 수 있다.
- **단일 리더 연결**: 현재 구현은 채널 인스턴스당 하나의 리더를 관리한다.
  다중 리더 시나리오는 `IksungReader` 인스턴스를 여러 개 생성하는 방식으로 처리한다.

---

## 고려했던 대안 (Alternatives Considered)

### PCSC NuGet 패키지

- `PCSC` (https://github.com/danm-de/pcsc-sharp) 는 macOS/Linux pcscd 도 지원하지만
  현재 타깃이 Windows 전용이므로 불필요한 의존성이 된다.
- SDK 패키지 정책(ADR-001: 의존성 최소화)과 충돌.

### SCardGetStatusChange 이벤트 모델

- `SCardGetStatusChange` 를 blocking 대기에 사용하면 카드 삽입/제거를 즉시 감지할 수 있다.
- 구현 복잡도가 높고(별도 스레드 관리, 재연결 처리) 현재 요구사항 범위를 초과한다.
- V2에서 polling → 이벤트 모델 전환을 개선 사항으로 남긴다.

---

## 결과 (Consequences)

- **긍정**: USB CCID 연결 IS-NFC 시리즈 리더기를 추가 드라이버 설치 없이 SDK 로 제어 가능
- **긍정**: `IIksungChannel` 교체 패턴 유지 — 상위 레이어 코드 수정 없음
- **부정**: Windows 전용 코드 증가 — 향후 크로스플랫폼 지원 시 `#if WINDOWS` 또는 런타임 체크 필요
- **중립**: `SCardSvr` 서비스 의존 — 배포 체크리스트에 서비스 활성화 항목 추가 필요
