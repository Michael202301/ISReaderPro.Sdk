# Sample 08 – NXP NTag 213 / 215 / 216

NTag 칩의 고급 기능을 보여줍니다:
  1. 활성화 + UID
  2. Get Version → 칩 타입 자동 감지 (NTag213/215/216)
  3. ECC 서명 읽기 (칩 진위 확인)
  4. 카운터 읽기
  5. Fast Read (전체 사용자 영역 한 번에 읽기)
  6. 페이지 쓰기 (사용자 영역)
  7. 패스워드 보호 설정 예제 (주석 처리됨, 신중하게 사용할 것)

NTag213 메모리 레이아웃 (45 pages, 180 bytes total):
  Page 0–1 : Serial number (UID)
  Page 2   : Internal / lock bytes
  Page 3   : Capability Container (CC)
  Page 4–39: User data (144 bytes)
  Page 40  : CFG 0 (AUTH0, ACCESS)
  Page 41  : CFG 1 (PWD, PACK)
  Page 42–44: (reserved)

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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/08-NTag213`
