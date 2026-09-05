# Serial (COM / USB) Channel — .NET Framework 4.7.2

시리얼(COM 포트 · USB 가상 COM) 채널로 IS 시리즈 리더기에 연결해 **버전 · 카드 UID · 버저 · RF On/Off · AutoRead**를 시연합니다.

> ⚠ net472는 **Windows 전용 · x86 빌드**입니다. (net472의 SerialPort는 x64에서 액세스 위반 버그가 있어 x86 필수)
> 포트는 **한 번에 한 프로그램만** 열 수 있습니다(독점) — 다른 프로그램(ISReaderPro 등)이 포트를 쓰고 있으면 먼저 닫으세요.

## 실행
```bash
SerialChannel.Net4x.exe COM4          # 기본 115200 bps
SerialChannel.Net4x.exe COM4 115200   # 보드레이트 지정
```
Visual Studio에서는 **솔루션을 열고 ▶ 시작** (플랫폼 = x86). 명령줄 인수는 프로젝트 속성 → 디버그에서 지정.

> PC/SC(CCID)로 연결하려면 `15-PcscChannel` 예제를 참고하세요. (연결 한 줄만 다르고 이후 API는 동일합니다.)

---
> 전체 SDK: [README](../../README.md) · MIT License
> .NET 8 버전: `../../NET8-Samples/SerialChannel`
