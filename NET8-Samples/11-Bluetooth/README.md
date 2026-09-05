# Sample 11 – Bluetooth / BLE 설정

IS-3500K의 BLE 모듈 설정을 읽고 변경하는 예제:
  1. 현재 BLE 장치 이름 읽기
  2. MAC 주소 읽기
  3. TX 파워 읽기
  4. BLE 이름 변경 (원래 이름으로 복원)
  5. Central 스캔 시작/중지 + 주변 장치 목록
  6. (주석) 시스템 리셋

주의:
  - BLE 이름 변경은 리더기 재시작 후 적용됩니다.
  - BleSystemResetAsync() 호출 시 연결이 끊어집니다.

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- COM3 115200 "MyReader"    (Serial, 이름 변경 후 복원)
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
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/11-Bluetooth`
