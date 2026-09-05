# Sample 09 – LF 125 kHz Advanced

다양한 LF 125 kHz 카드 포맷을 읽는 예제입니다:
  - EM410X  : 공장/출입 관리 시스템에서 가장 흔한 포맷 (5바이트 UID)
  - ISO 11784/11785 FDX-B : 동물 ID 칩 (15자리 국가코드 + 개체번호)
  - T5577   : 국내 보안 시스템 전용 블록 포맷
  - Temic   : T5577 범용 재기록 가능 LF 칩
  - Raw bits: 원시 RF 비트 스트림
  - HTRC 자동 튜닝

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 em410x        (Serial, 특정 포맷만 반복)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/09-Lf125KhzAdvanced`
