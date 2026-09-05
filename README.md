# Iksung.Reader — 익성전자 NFC/RFID 리더 공식 .NET SDK

익성전자 IS-3500 시리즈 리더를 **Serial · PC/SC(USB CCID) · TCP** 어느 쪽으로 연결하든 **같은 코드**로 다루는 .NET 라이브러리입니다.

- **라이선스** MIT — 상업적 이용 포함 무료, 로열티 없음
- **대상 프레임워크** `net8.0` · `net472` (레거시 WinForms 통합용)
- **동작 환경** Windows · Linux · macOS
- **상태** 프리릴리스 `0.1.0-preview`

> 익성전자 홈페이지 <http://www.iksung.co.kr>

---

## 설치

```bash
dotnet add package Iksung.Reader --prerelease
```

프리릴리스이므로 `--prerelease`가 필요합니다. 정식 `1.0.0` 이후에는 이 옵션 없이 설치됩니다.

**인터넷이 차단된 환경(폐쇄망)** 이라면 GitHub Releases의 오프라인 통합팩(zip)을 받아 USB로 반입하시면 됩니다. 의존 패키지가 모두 들어 있어 인터넷 없이 빌드됩니다.

---

## 5분 안에 첫 UID 읽기

```csharp
using Iksung.Reader;

// 포트를 몰라도 됩니다 — 시리얼을 먼저 훑고, 없으면 PC/SC 리더를 찾습니다.
var ports   = await IksungReaderDiscovery.ScanIksungPortsAsync();
var pcsc    = IksungPcscDiscovery.GetAvailableReaders();

await using var reader =
      ports.Count > 0 ? await IksungReader.ConnectSerialAsync(ports[0])
    : pcsc.Count  > 0 ? await IksungReader.ConnectPcscAsync(pcsc[0])
    : throw new InvalidOperationException("리더를 찾지 못했습니다.");

Console.WriteLine($"펌웨어: {await reader.ReadVersionAsync()}");

byte[] uid = await reader.ReadAllUidAsync();   // 카드를 리더에 올려두세요
Console.WriteLine($"UID: {BitConverter.ToString(uid).Replace("-", "")}");
```

연결 방법만 다르고 그다음 코드는 전부 같습니다.

```csharp
// 시리얼 (COM 포트 · USB 가상 COM)
await IksungReader.ConnectSerialAsync("COM40", 115200);

// PC/SC (USB CCID) — 리더 이름은 IksungPcscDiscovery.GetAvailableReaders() 로 조회
await IksungReader.ConnectPcscAsync("iksung IS-3500Z 0");

// TCP (이더넷 리더)
await IksungReader.ConnectSocketAsync("192.168.0.50", 4000);
```

---

## 무엇을 할 수 있나

| 분야 | 기능 |
|---|---|
| 카드 인식 | ISO 14443-A/B · ISO 15693 · LF 125 kHz 통합 UID 읽기, AutoRead(이벤트 기반) |
| MIFARE | Classic 인증·블록 R/W, Ultralight 페이지 R/W, NTAG 213/215/216(버전·서명·카운터) |
| DESFire | EV1/EV2/EV3 애플리케이션·파일 워크플로 |
| USIM | ISO 7816 ATR · TPDU · ICCID |
| 장치 제어 | 부저 · RF On/Off · 릴레이 출력 · 고유 ID · 펌웨어 버전 |
| Bluetooth | 장치 이름 · MAC · TX 파워 · GAP · 스캔 · 연결 상태 진단 |
| 저수준 | `SendRawCommandAsync` 로 임의 명령 전송, TX/RX 원시 프레임 캡처 |

전체 API는 [매뉴얼](https://github.com/Michael202301/ISReaderPro.Sdk/blob/main/docs/manual/README.md)과 [API 레퍼런스](https://github.com/Michael202301/ISReaderPro.Sdk/blob/main/docs/manual/api-reference.md)를 보세요.

---

## 예제

`NET8-Samples/` 와 `NET4x-Samples/` 에 각각 17종이 들어 있습니다. **예제 폴더 하나만 복사해도 그대로 동작합니다.**

```bash
dotnet run --project NET8-Samples/01-ReadAnyUid              # 리더 자동 탐색
dotnet run --project NET8-Samples/01-ReadAnyUid -- COM40     # 포트 지정
dotnet run --project NET8-Samples/01-ReadAnyUid -- pcsc      # PC/SC
```

| 번호 | 예제 | 내용 |
|---|---|---|
| — | HelloWorld.Console | 연결 · 버전 · 고유 ID (여기부터 시작하세요) |
| 01 | ReadAnyUid | UID 폴링 (ISO14443A/B · ISO15693 · LF) |
| 02 | Iso14443a | Layer-3/4 + APDU |
| 03 | MifareClassic | 인증 + 블록 읽기/쓰기 |
| 04 | MifareUltralight | 페이지 덤프/쓰기 |
| 05 | Iso15693 | 멀티블록 읽기 |
| 06 | AutoRead | 이벤트 기반 자동 인식 |
| 07 | Desfire | DESFire 전체 워크플로 |
| 08 | NTag213 | NTAG 버전·서명·카운터 |
| 09 | Lf125KhzAdvanced | EM410X · ISO11784 · T5577 |
| 10 | Iso7816 | USIM ATR · TPDU · ICCID |
| 11 | Bluetooth | BLE 설정 조회/변경 |
| 12 | Relay | 릴레이 I/O 제어 |
| 13 | CommandConsole | 대화형 RAW 명령 콘솔 |
| 14 | SystemFeatures | 버전 · 고유 ID · 부저 · RF |
| 15 | PcscChannel | PC/SC 채널 직접 사용 |
| — | SerialChannel | 시리얼 채널 직접 사용 |

> **.NET Framework 예제는 x86으로 빌드해야 합니다.** `net472`의 `SerialPort`가 x64에서 액세스 위반으로 죽는 알려진 문제 때문입니다.
> `dotnet run --project NET4x-Samples/01-ReadAnyUid -p:Platform=x86 -- COM40`

---

## 코딩 없이 쓰는 방법 (HID 키보드 모드)

리더를 **HID 키보드 모드**로 두면 카드를 태그할 때 UID가 키보드 입력처럼 들어갑니다(끝에 Enter 옵션). 메모장·엑셀·기존 업무 프로그램에 **코드 한 줄 없이** 바로 붙습니다. 개발자가 없는 현장에서는 이 방법이 가장 빠릅니다. 설정은 ISReaderPro 도구에서 합니다.

---

## 플랫폼별 준비물

| OS | 준비물 |
|---|---|
| Windows | 별도 준비 없음. PC/SC 사용 시 `SCardSvr`(스마트 카드) 서비스 실행 |
| Linux | 시리얼 권한: `sudo usermod -a -G dialout $USER` (재로그인 필요). PC/SC 사용 시 `pcscd` |
| macOS | 별도 준비 없음 (`/dev/cu.usbserial-*`) |

---

## 잘 안 될 때

**"포트를 열 수 없습니다"**
포트 이름이 다를 가능성이 큽니다. 예제를 **인수 없이** 실행하면 리더가 붙은 포트를 직접 찾아줍니다. `IksungReaderDiscovery.ScanIksungPortsAsync()` 가 같은 일을 합니다.

**PC/SC 리더가 목록에 안 보임**
리더는 **CCID 모드일 때만** PC/SC로 잡힙니다. 가상 COM(CDC) 모드면 시리얼로만 접속됩니다. ISReaderPro에서 인터페이스를 CCID로 바꾸고 USB를 다시 연결하세요. 두 모드는 동시에 쓸 수 없습니다.

**`SW=6300` 오류**
대부분 **안테나에 카드가 없는 상태**입니다. 리더 고장이 아닙니다. 카드를 올려둔 채 다시 시도하세요.

**포트를 열 수 없다는데 이름은 맞음**
시리얼 포트는 한 번에 한 프로그램만 열 수 있습니다. ISReaderPro 등 다른 프로그램이 잡고 있으면 먼저 닫으세요.

---

## 문의

- **기술 이슈·버그·기능 요청** — GitHub Issues
- **견적 · 구매 · 납품 문의** — <jch1002@iksung.co.kr> · 070-8237-5078

---

## English summary

`Iksung.Reader` is the official .NET SDK for IKSUNG NFC/RFID readers (IS-3500 series).
One API over three transports — **Serial, PC/SC (USB CCID) and TCP** — targeting `net8.0` and `net472`,
running on Windows, Linux and macOS.

```bash
dotnet add package Iksung.Reader --prerelease
```

```csharp
using Iksung.Reader;

var ports = await IksungReaderDiscovery.ScanIksungPortsAsync();
await using var reader = await IksungReader.ConnectSerialAsync(ports[0]);
byte[] uid = await reader.ReadAllUidAsync();
```

Supports ISO 14443-A/B, ISO 15693, LF 125 kHz, MIFARE Classic/Ultralight/NTAG, DESFire EV1-EV3,
ISO 7816 (USIM), AutoRead events, BLE configuration and relay control.
MIT licensed — free for commercial use, no royalties.

---

## 라이선스

MIT — [LICENSE.md](https://github.com/Michael202301/ISReaderPro.Sdk/blob/main/LICENSE.md)
