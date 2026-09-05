# Sample 07 – Mifare DESFire EV1/EV2

DESFire 카드의 전체 워크플로를 보여줍니다:
  1. 카드 활성화 + UID 읽기
  2. 인증 키 저장 (AES-128 기본 키: 16 × 0x00)
  3. Master Key 인증 (Key #0, AES)
  4. 여유 메모리 조회
  5. Application ID 목록 조회
  6. 기존 Application 선택 (또는 Root 선택)
  7. 파일 ID 목록 조회
  8. Standard File 읽기 / 쓰기

⚠️  주의: 실제 카드 데이터를 변경합니다. 테스트용 카드를 사용하세요.
          Master Key가 기본값(16 × 0x00)이 아닌 경우 인증이 실패합니다.

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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/07-Desfire`
