# Sample 02 – ISO 14443-A / ISO-DEP (T=CL)

ISO 14443-A 카드를 활성화하고 APDU 교환 예제를 보여줍니다.
스마트카드(NFC Forum Type 4, ISO-DEP)가 있으면 SELECT PPSE APDU도 전송합니다.

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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/02-Iso14443a`
