# PC/SC (CCID) 채널 사용 가이드

## 1. 개요

PC/SC(Personal Computer/Smart Card) 채널은 USB CCID 모드로 동작하는 IS-NFC 시리즈 리더기를
Windows Smart Card Service(SCardSvr)를 통해 제어하는 방법입니다.

IS-3500Z, IS-3500K 등 USB CCID 인터페이스를 갖춘 리더기는 시리얼 케이블 없이
USB 케이블만으로 PC에 연결할 수 있으며, 별도 드라이버 설치 없이 Windows 내장
USB HID/CCID 드라이버로 동작합니다.

Iksung.Reader SDK는 내부적으로 `winscard.dll` P/Invoke를 사용하여 PC/SC 통신을 처리하므로
외부 NuGet 패키지 의존성이 없습니다. STX/ETX 패킷과 ISO 7816-4 APDU 사이의 변환도
SDK 내부에서 자동으로 수행되므로 상위 레이어 코드는 Serial/Socket 채널과 동일하게 작성할 수 있습니다.

---

## 2. 요구사항

| 항목 | 요구 사항 |
|------|-----------|
| 운영체제 | Windows 10 / Windows 11 (Windows 전용) |
| .NET 버전 | .NET 8 이상, 또는 .NET Framework 4.7.2 이상 |
| TargetFramework | `net8.0-windows` 또는 `net472` |
| Smart Card Service | `SCardSvr` (스마트 카드 서비스) 실행 중 |
| 빌드 플랫폼 | x86 권장 (winscard.dll 호환성) |

### Smart Card Service 활성화 확인 방법

```
Win + R → services.msc → "스마트 카드" 검색 → 시작 유형: 자동, 상태: 실행 중 확인
```

또는 PowerShell:

```powershell
Get-Service SCardSvr | Select-Object Status, StartType
# Status: Running, StartType: Manual 또는 Automatic 이어야 합니다.

# 서비스 시작 (관리자 권한 필요)
Start-Service SCardSvr
```

---

## 3. 리더 검색

`IksungPcscDiscovery.GetAvailableReaders()` 를 사용하여 현재 시스템에 연결된
PC/SC 리더 목록을 조회합니다.

```csharp
using Iksung.Reader;

var readers = IksungPcscDiscovery.GetAvailableReaders();

if (readers.Count == 0)
{
    Console.WriteLine("연결된 리더가 없습니다.");
    return;
}

foreach (var name in readers)
    Console.WriteLine(name);
// 출력 예: "iksung IS-3500Z 0"
//          "ACS ACR39U ICC Reader 0"
```

Smart Card Service 가 중지된 경우 빈 목록을 반환합니다 (예외를 던지지 않음).

---

## 4. 리더 Hot-plug 모니터링

`IksungPcscDiscovery.StartMonitor()` 를 호출하면 백그라운드에서 1초마다 리더 목록을
폴링하고, 변경이 감지되면 `ReaderListChanged` 이벤트를 발사합니다.

```csharp
// 모니터링 시작 (애플리케이션 시작 시)
IksungPcscDiscovery.ReaderListChanged += (_, readers) =>
{
    Console.WriteLine($"리더 목록 변경: {readers.Count}개");
    foreach (var r in readers)
        Console.WriteLine($"  {r}");
};
IksungPcscDiscovery.StartMonitor(pollIntervalMs: 1000);

// 모니터링 중지 (애플리케이션 종료 시)
IksungPcscDiscovery.StopMonitor();
```

폴링 간격(`pollIntervalMs`)의 기본값은 1000ms(1초)입니다.
WPF/WinForms 앱에서 이벤트 핸들러 안에서 UI를 업데이트할 경우,
이벤트는 백그라운드 스레드에서 발사되므로 `Dispatcher.Invoke` 또는 `BeginInvoke`가 필요합니다.

---

## 5. 연결

`IksungReader.ConnectPcscAsync()` 팩토리 메서드로 PC/SC 리더에 연결합니다.

```csharp
using Iksung.Reader;
using Iksung.Reader.Exceptions;

string readerName = "iksung IS-3500Z 0"; // GetAvailableReaders() 결과에서 선택

try
{
    await using var reader = await IksungReader.ConnectPcscAsync(readerName);
    Console.WriteLine($"연결 성공! 채널: {reader.ConnectedVia}"); // ChannelType.Pcsc

    // 이후 명령 전송
    var info = await reader.GetReaderInfoAsync();
    Console.WriteLine($"FW: {info.FirmwareVersion}");
}
catch (ChannelDisconnectedException ex)
{
    Console.WriteLine($"연결 실패: {ex.Message}");
}
```

연결 옵션(`IksungReaderOptions`)을 지정할 수도 있습니다:

```csharp
var options = new IksungReaderOptions { LogRawPackets = true };
await using var reader = await IksungReader.ConnectPcscAsync(readerName, options);
```

> **주의**: 일부 PC/SC 리더는 카드/태그가 삽입된 상태에서만 `SCardConnect`가 성공합니다.
> 리더 모델에 따라 다르므로 카드를 올려놓은 상태에서 연결을 시도하세요.

---

## 6. ATR 조회

ATR(Answer To Reset)은 카드가 리셋될 때 반환하는 정보 바이트로, 카드 타입과 프로토콜을 식별합니다.
SDK는 `SCardConnect` 직후 `SCardStatus` 를 통해 ATR을 자동으로 캐싱합니다.

`ConnectedVia == ChannelType.Pcsc` 임을 확인한 후 내부 채널에 접근하는 방법은
현재 V1 공개 API에서는 제공되지 않습니다. ATR이 필요한 경우 `SendRawCommandAsync` 를
사용하거나 SDK 향후 버전에서 제공 예정인 `reader.GetAtrAsync()` 를 사용하세요.

연결 성공 자체로 카드 타입이 인식된 것으로 간주하고,
이후 명령으로 카드 정보를 조회하는 것을 권장합니다.

---

## 7. 기본 명령 실행

PC/SC 채널로 연결한 후에는 Serial/Socket 채널과 동일한 API를 사용합니다.
SDK 내부에서 STX/ETX 패킷을 ISO 7816-4 APDU로 자동 변환합니다.

```csharp
await using var reader = await IksungReader.ConnectPcscAsync(readerName);

// 리더 정보 읽기 (FirmwareVersion · Channel · IsConnected · PortOrAddress)
ReaderInfo info = await reader.GetReaderInfoAsync();
Console.WriteLine($"FW: {info.FirmwareVersion} | 채널: {info.Channel} | 리더: {info.PortOrAddress}");

// 카드 UID 읽기 (카드가 리더 위에 있어야 함 · 카드 종류 자동 감지)
// ReadAllUidAsync 는 byte[] 1건을 반환합니다 (현재 필드에 있는 카드의 UID)
byte[] uid = await reader.ReadAllUidAsync();
string hex = BitConverter.ToString(uid).Replace("-", " ");
Console.WriteLine($"UID: {hex} ({uid.Length} bytes)");

// 카드 종류를 별도로 조회하고 싶을 때
byte[] cardType = await reader.ReadAllCardTypeAsync();
Console.WriteLine($"카드 타입 코드: {BitConverter.ToString(cardType)}");

// 부저
await reader.BuzzerAsync();         // 기본 200ms (durationUnit=2)
// await reader.BuzzerAsync(5);     // 500ms

// RF 제어
await reader.RfOffAsync();
await Task.Delay(500);
await reader.RfOnAsync();
```

---

## 8. ISO 7816-4 APDU 변환 규칙

SDK 내부에서 STX/ETX 패킷을 ISO 7816-4 APDU로 자동 변환합니다.
상위 레이어에서는 이 변환을 신경 쓸 필요가 없으나, 로우 레벨 디버깅 시 참고하세요.

| Case | 조건 | APDU 구조 | 설명 |
|------|------|-----------|------|
| Case 2 (Short) | 요청 Data 없음 | `FF CMD1 CMD2 00 00` | Le=00: 최대 256byte 응답 기대 |
| Case 3 (Short) | 요청 Data 있음, ≤255 bytes | `FF CMD1 CMD2 00 Lc Data` | SW만 응답 |
| Case 3 (Extended) | 요청 Data 있음, >255 bytes | `FF CMD1 CMD2 00 00 LcH LcL Data` | 부트로더 전용 |

- `CLA = 0xFF` — IKSUNG 벤더 클래스
- `INS = CMD1` — 명령 Major 바이트
- `P1 = CMD2` — 명령 Minor 바이트 (buzzer 비트 포함)
- `P2 = 0x00`

응답 마지막 2바이트는 SW1/SW2입니다. `SW = 90 00` 이 아니면 `IksungProtocolException`을 던집니다.

---

## 9. AutoRead

PC/SC 는 request-response 모델로만 동작합니다. 리더기가 자발적으로 패킷을 푸시하는
Serial AutoRead 모드와 달리, PC/SC 에서의 AutoRead 는 SDK 내부 폴링으로 구현됩니다.

```csharp
reader.TagDetected += (_, e) =>
{
    // TagDetectedEventArgs:
    //   - CardType  : 감지된 카드/태그 종류 (CardType enum)
    //   - Uid       : byte[] UID
    //   - UidHex    : string (하이픈 없이 대문자, 편의용)
    //   - Cmd1/Cmd2 : 응답 cmd 바이트 (raw)
    //   - RawData   : 펌웨어 응답 전체 페이로드
    Console.WriteLine($"태그 감지! UID={e.UidHex} (타입: {e.CardType})");
};

await reader.StartAutoReadAsync();
// ... 대기 (예: Console.ReadKey)
await reader.StopAutoReadAsync();
```

> **V1 제약사항**: PC/SC 채널에서 AutoRead 는 `ReadAllUid` 명령을 반복 폴링하는 방식으로
> 동작합니다. Serial 채널의 하드웨어 push 방식보다 응답 지연이 있을 수 있습니다.
> V2에서 `SCardGetStatusChange` 이벤트 모델 기반 개선이 계획되어 있습니다.

---

## 10. 펌웨어 업데이트 / Raw 명령

PC/SC 채널도 Serial 과 동일하게 <code>SendRawCommandAsync</code> 를 통해 임의의 STX/ETX
명령(CMD1·CMD2·Data) 을 직접 송신할 수 있습니다. SDK 내부에서 ISO 7816-4 APDU 로 자동
변환됩니다 (§8 참조).

```csharp
await using var reader = await IksungReader.ConnectPcscAsync(readerName);

// 예: Common 버전 명령을 raw 로 직접 송신
byte[] resp = await reader.SendRawCommandAsync(
    cmd1: 0x00,      // MAJOR_COMMON
    cmd2: 0x00,      // COMMON_VERSION
    data: null,
    timeoutMs: 1000);
Console.WriteLine($"버전: {System.Text.Encoding.ASCII.GetString(resp).Trim()}");
```

> **펌웨어 업데이트** — IS-3500K V6.0/V7.0 부트로더 기반 양산 펌웨어 업데이트는
> 별도 도구 (<code>IksungFirmwareUpdater</code>) 가 내부적으로 처리하며, 일반 응용에서
> Raw I/O 모드를 직접 다룰 필요는 없습니다.

---

## 11. 오류 처리

| 예외 | 원인 | 대처 방법 |
|------|------|-----------|
| `ChannelDisconnectedException` | PC/SC 연결 실패 / 끊김 | 리더 이름 확인, 카드 삽입 여부 확인 |
| `IksungProtocolException` | SW ≠ 90 00 | 리더기 로그 확인, 명령 파라미터 검토 |
| `IksungTimeoutException` | 응답 타임아웃 | 타임아웃 값 증가, 카드 위치 확인 |
| `OperationCanceledException` | CancellationToken 취소 | 정상 취소 흐름 |

Smart Card Service 관련 오류 코드:

| HRESULT | 의미 |
|---------|------|
| `0x8010001D` `SCARD_E_NO_SERVICE` | Smart Card Service 중지됨 |
| `0x80100007` `SCARD_E_SERVICE_STOPPED` | Service 중지됨 (연결 중 발생) |
| `0x8010002E` `SCARD_E_NO_READERS_AVAILABLE` | 리더 없음 |

이 경우 `GetAvailableReaders()` 는 예외 없이 빈 목록을 반환합니다.

---

## 12. 문제 해결 (FAQ)

**Q. `GetAvailableReaders()` 가 빈 목록을 반환합니다.**

- USB CCID 리더가 연결되어 있는지 확인하세요.
- 장치 관리자에서 "스마트 카드 판독기" 범주에 리더가 표시되는지 확인하세요.
- Smart Card Service(`SCardSvr`)가 실행 중인지 확인하세요.
- Windows가 CCID 드라이버를 인식하지 못하는 경우 USB 포트를 변경해 보세요.

**Q. `ConnectPcscAsync` 가 실패합니다 (`ChannelDisconnectedException`).**

- PC/SC 리더는 카드가 삽입된 상태에서만 `SCardConnect`에 성공하는 경우가 있습니다.
  IS-3500Z 등 NFC 리더는 태그를 리더 위에 올려놓은 상태에서 연결을 시도하세요.

**Q. AutoRead 이벤트가 느립니다.**

- PC/SC 채널은 폴링 방식이므로 Serial 채널보다 지연이 있습니다.
  태그 감지 간격을 줄이려면 AutoRead 폴링 주기 설정을 확인하세요.

**Q. `net8.0` 으로 빌드하면 컴파일 오류가 발생합니다.**

- `winscard.dll` P/Invoke 코드는 Windows 전용입니다.
  프로젝트의 `TargetFramework`를 `net8.0-windows` 로 변경하세요.

**Q. 64비트 빌드에서 동작하지 않습니다.**

- `PlatformTarget`을 `x86`으로 설정하면 32비트로 빌드되어 호환성이 높습니다.
  64비트로 빌드하는 경우 `winscard.dll` 경로(`System32` vs `SysWOW64`)를 확인하세요.
  일반적으로 x86 빌드를 권장합니다.

**Q. 여러 리더가 있을 때 특정 리더에 연결하려면?**

```csharp
var readers = IksungPcscDiscovery.GetAvailableReaders();
// 이름에 "iksung" 이 포함된 첫 번째 리더 선택
string? target = readers.FirstOrDefault(r => r.Contains("iksung"));
if (target != null)
    await using var reader = await IksungReader.ConnectPcscAsync(target);
```
