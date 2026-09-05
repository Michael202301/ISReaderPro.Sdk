# Sample 10 – ISO 7816 USIM / Smart Card

리더기 내부 USIM 슬롯(또는 외부 스마트카드)을 이용한 ISO 7816 T=0 통신 예제:
  1. 카드 활성화 → ATR 수신
  2. SELECT MF (Master File) APDU
  3. SELECT EF_ICCID (파일 선택)
  4. READ BINARY → ICCID 읽기
  5. 카드 비활성화

ICCID는 SIM 카드의 고유 식별번호 (20자리)입니다.

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 0             (Serial, 채널 0)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/10-Iso7816`
