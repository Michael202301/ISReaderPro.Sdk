# Serial (COM / USB) Channel

시리얼(COM 포트 · USB 가상 COM) 채널로 IS 시리즈 리더기에 연결해 **버전 · 카드 UID · 버저 · RF On/Off · AutoRead**를 시연합니다.

> 시리얼은 **크로스플랫폼**입니다 (Windows · Linux · macOS).
> 포트는 **한 번에 한 프로그램만** 열 수 있습니다(독점) — 다른 프로그램(ISReaderPro 등)이 포트를 쓰고 있으면 먼저 닫으세요.

## 실행
```bash
dotnet run -- COM4                     # Windows
dotnet run -- COM4 115200              # 보드레이트 지정
dotnet run -- /dev/ttyUSB0             # Linux
dotnet run -- /dev/tty.usbserial-XXXX  # macOS
```

> PC/SC(CCID)로 연결하려면 `15-PcscChannel` 예제를 참고하세요. (연결 한 줄만 다르고 이후 API는 동일합니다.)

---
> 전체 SDK: [README](../../README.md) · MIT License
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/SerialChannel`
