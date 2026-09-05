# Sample 06 – AutoRead (Event-Driven Card Detection)

리더기의 AutoRead 폴링 모드를 사용합니다.
카드를 감지하면 리더기가 자동으로 패킷을 전송하고,
SDK가 TagDetected 이벤트로 알려줍니다.

AutoRead는 여러 카드 타입(ISO14443A/B, ISO15693, Felica, LF)을
동시에 폴링하므로 폴링 주기 설정이 필요 없습니다.

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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/06-AutoRead`
