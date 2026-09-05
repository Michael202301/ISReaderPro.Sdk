# Sample 13 – Interactive Command Console

IS-3500K에 RAW 명령을 대화형으로 보내고 응답을 확인하는 디버그 콘솔.
프로토콜을 직접 탐색하거나 새 명령을 테스트할 때 유용합니다.

명령 형식:
  <CMD1_hex> <CMD2_hex> [DATA_hex...]   예: 00 10            (버전 읽기)
                                            01 10            (ISO14443A 활성화)
                                            02 20 60 00 01   (Mifare 인증)

내장 단축 명령:
  version   — 펌웨어 버전 읽기
  uid       — UID 읽기
  rfon      — RF On
  rfoff     — RF Off
  help      — 도움말
  quit / q  — 종료

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200               (Serial, 보드레이트 지정)
  dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
  dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)

## 실행
```bash
dotnet run -- COM3                 # Windows (Serial)
dotnet run -- /dev/ttyUSB0         # Linux (Serial)
dotnet run -- pcsc                 # PC/SC (USB CCID)
```

---
> 전체 SDK: [README](../../README.md) · MIT License
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/13-CommandConsole`
