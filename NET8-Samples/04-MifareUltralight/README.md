# Sample 04 – Mifare Ultralight / NTag

Mifare Ultralight C 또는 NTag 카드를 활성화하고
페이지를 읽고 쓰는 예제입니다.

페이지 구조 (MF ULC):
  0–1: Serial number / check byte
  2:   Lock bytes (read only)
  3:   OTP
  4~:  User data pages (4 bytes each)

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 5             (Serial, 대상 페이지 5)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/04-MifareUltralight`
