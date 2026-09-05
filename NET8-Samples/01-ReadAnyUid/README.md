# Sample 01 – Read Any UID

리더기에 연결해 NFC/RFID 카드 UID를 계속 읽어서 출력합니다.
ISO 14443-A/B, ISO 15693, LF 125 kHz 순서로 시도합니다.

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/01-ReadAnyUid`
