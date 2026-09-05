# Sample 05 – ISO 15693 (Vicinity Cards / iCODE)

ISO 15693 (13.56 MHz vicinity) 카드를 활성화하고
여러 블록을 읽는 예제입니다.
대표 카드: NXP ICODE SLI/SLI-S, Texas Instruments Tag-it HF-I

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 0 8           (Serial, 블록 0부터 8블록 읽기)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/05-Iso15693`
