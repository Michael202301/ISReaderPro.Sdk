# IKSUNG Reader SDK for .NET

> Easy-to-use SDK for IKSUNG NFC/RFID readers. Works on Windows, Linux, and macOS.

🌐 **[www.iksung.co.kr](http://www.iksung.co.kr)**

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)

## ⚠️ Status

**Pre-release / under active development.** First public NuGet target: `v1.0.0`.

---

## Install

```bash
dotnet add package Iksung.Reader
```

(Coming soon — see [Status](#%EF%B8%8F-status))

---

## Hello World

```csharp
using Iksung.Reader;

await using var reader = await IksungReader.ConnectSerialAsync("COM3");
Console.WriteLine($"Firmware: {await reader.ReadVersionAsync()}");

byte[] uid = await reader.ReadIso14443aUidAsync();
Console.WriteLine($"UID: {BitConverter.ToString(uid).Replace("-", "")}");
```

---

## Supported Products

| 제품 | 인터페이스 |
|------|-----------|
| IS-NFC | Serial (UART 115200) |
| IS-NFC 시리즈 | PC/SC |
| IS-NFC 시리즈 | TCP Socket |

---

## Supported Channels

| Channel | Status |
|---------|--------|
| Serial (UART) | ✅ 지원 |
| TCP/IP Socket | ✅ 지원 |
| USB CCID (PC/SC) | 🔜 예정 |

---

## Platform Support

| OS | 지원 | Serial 포트 예시 |
|----|------|----------------|
| Windows | ✅ | `COM3` |
| Linux | ✅ | `/dev/ttyUSB0` |
| macOS | ✅ | `/dev/cu.usbserial-1234` |

### Linux 설정

```bash
# 시리얼 포트 권한 추가 (로그아웃 후 재로그인 필요)
sudo usermod -a -G dialout $USER
```

---

## Samples

| 번호 | 예제 | 설명 |
|------|------|------|
| 01 | ReadAnyUid | UID 폴링 (ISO14443A/B, ISO15693, LF) |
| 02 | Iso14443a | ISO 14443-A Layer-3/4 + APDU |
| 03 | MifareClassic | 인증 + 블록 읽기/쓰기 |
| 04 | MifareUltralight | 페이지 덤프/쓰기 |
| 05 | Iso15693 | 멀티블록 읽기 |
| 06 | AutoRead | 이벤트 기반 자동 인식 |
| 07 | Desfire | DESFire EV1/EV2/EV3 전체 워크플로 |
| 08 | NTag213 | NTag213/215/216 버전/서명/카운터 |
| 09 | Lf125KhzAdvanced | EM410X, ISO11784, SECOM, Temic |
| 10 | Iso7816 | USIM ATR + TPDU + ICCID 읽기 |
| 11 | Bluetooth | BLE 이름/MAC/TX파워/GAP 설정 |
| 12 | Relay | 릴레이 I/O 제어 |
| 13 | CommandConsole | 대화형 RAW 명령 콘솔 |

### .NET Framework 4.7.2 예제

| 번호 | 예제 | 설명 |
|------|------|------|
| net4x/01 | HelloWorld | 연결 + UID 폴링 |
| net4x/02 | MifareClassic | 인증 + 블록 읽기/쓰기 |
| net4x/03 | AutoRead | 이벤트 기반 감지 |
| net4x/04 | WinFormsIntegration | WinForms `Invoke()` 패턴 |

```bash
# 실행 예시
dotnet run --project samples/01-ReadAnyUid -- COM3
dotnet run --project samples/net4x/01-HelloWorld -- COM3
```

---

## Documentation

| 문서 | 내용 |
|------|------|
| [시작하기](docs/manual/01-getting-started.md) | 설치, 연결, 첫 번째 카드 읽기 |
| [API 레퍼런스](docs/manual/api-reference.md) | 전체 공개 메서드 목록 |
| [.NET 4.x 가이드](docs/manual/net4x-guide.md) | WinForms/WPF 통합 패턴 |

---

## License

MIT — see [LICENSE.md](LICENSE.md).

Free for commercial and non-commercial use. No royalties, no fees.

---

## Related

- 🌐 [익성전자 공식 홈페이지](http://www.iksung.co.kr)
- [Springcard PC/SC SDK](https://github.com/springcard/springcard.pcsc.sdk) — Inspiration for clean SDK design
