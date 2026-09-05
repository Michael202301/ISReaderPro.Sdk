# Sample 03 – Mifare Classic 1K / 4K

Mifare Classic 카드를 활성화하고, 기본 키로 인증 후 블록을 읽고 씁니다.
주의: 실제 카드 데이터를 변경하므로 테스트용 카드를 사용하세요.

기본 키: FF FF FF FF FF FF (Mifare Classic 초기 공장 키)

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 4             (Serial, 블록 4 대상)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/03-MifareClassic`
