# Sample 12 – Relay / Digital I/O

IS-3500K의 릴레이 모듈 제어 예제:
  1. 모든 입력 핀(DIN 1~5) 상태 읽기
  2. 모든 릴레이 출력(RELAY 1~8) 상태 읽기
  3. 릴레이 순차 ON (1→2→3→…→8, 300ms 간격)
  4. 모든 릴레이 OFF
  5. 릴레이 순차 OFF (8→7→6→…→1, 300ms 간격)
  6. 전체 ON / 전체 OFF 토글
  7. 자동 꺼짐 타이머 설정 예시 (Relay 1, 2000ms)
  8. 입력 실시간 모니터링 루프

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 monitor       (Serial, 입력 모니터링 전용)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/12-Relay`
