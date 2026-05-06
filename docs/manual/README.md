# Iksung.Reader SDK — 사용 설명서

익성전자 NFC/RFID 리더기용 .NET SDK 공식 문서입니다.

---

## 문서 목차

| 문서 | 내용 |
|------|------|
| [01. 시작하기](01-getting-started.md) | 설치, 연결, Hello World |
| [02. 공통 명령](02-common-commands.md) | 버전 읽기, RF On/Off, UID |
| [03. ISO 14443 A/B](03-iso14443ab.md) | 비접촉 카드 활성화 및 APDU 교환 |
| [04. Mifare Classic](04-mifare-classic.md) | 인증, 블록 읽기/쓰기 |
| [05. Mifare Ultralight / NTag](05-mifare-ultralight-ntag.md) | 페이지 읽기/쓰기, NTag 고급 기능 |
| [06. ISO 15693](06-iso15693.md) | HF 태그 멀티블록 읽기/쓰기 |
| [07. DESFire EV1/EV2/EV3](07-desfire.md) | 앱/파일 관리, AES 인증, 트랜잭션 |
| [08. LF 125 kHz](08-lf125khz.md) | EM410X, ISO11784, SECOM, Temic |
| [09. AutoRead 이벤트 모드](09-autoread.md) | 이벤트 기반 카드 자동 감지 |
| [10. ISO 7816 / USIM](10-iso7816-usim.md) | SIM 카드 T=0 통신, ICCID 읽기 |
| [11. Bluetooth / BLE](11-bluetooth.md) | BLE 이름·TX 파워·GAP 파라미터 설정 |
| [12. 릴레이 보드](12-relay.md) | 디지털 입력 감지, 릴레이 On/Off |
| [13. 예외 처리 및 고급 사용](13-error-handling.md) | 예외 종류, Raw 명령, 취소 토큰 |
| [API 레퍼런스](api-reference.md) | 전체 공개 메서드 목록 |
| [.NET Framework 4.x 가이드](net4x-guide.md) | WinForms/WPF 통합, await using 대체 패턴 |

---

## 지원 대상

| 제품 | 인터페이스 |
|------|-----------|
| IS-NFC | Serial (UART 115200) |
| IS-NFC 시리즈 | PC/SC |
| IS-NFC 시리즈 | TCP Socket |

---

## 최소 요구사항

- .NET 8.0 이상
- Windows / macOS / Linux (크로스 플랫폼)
- `Iksung.Reader` NuGet 패키지

---

## 빠른 예제

```csharp
using Iksung.Reader;

await using var reader = await IksungReader.ConnectSerialAsync("COM3");
Console.WriteLine(await reader.ReadVersionAsync());

byte[] uid = await reader.ReadIso14443aUidAsync();
Console.WriteLine(BitConverter.ToString(uid));
```

---

© 익성전자 (Iksung Electronics). MIT License.
