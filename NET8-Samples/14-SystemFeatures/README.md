# Sample 14 – System Features

시스템 레벨 기능 예제:
  A. 포트 탐색     — IksungReaderDiscovery.ScanIksungPortsAsync
  B. 연결 옵션     — IksungReaderOptions (AutoReconnect, LogRawPackets)
  C. 연결 상태 이벤트 — reader.ConnectionChanged
  D. 리더기 정보   — reader.GetReaderInfoAsync, reader.PingAsync
  E. Raw 패킷 로그 — reader.RawPacketReceived
  F. 명시적 해제   — reader.DisconnectAsync

사용법:
  dotnet run -- COM3                      (Serial — Windows)
  dotnet run -- /dev/ttyUSB0              (Serial — Linux)
  dotnet run -- scan                      (Serial 포트 탐색 모드)
  dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
  dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
  dotnet run -- pcsc-list                 (PC/SC 리더 목록 출력)

## 실행
```bash
dotnet run -- COM3                 # Windows (Serial)
dotnet run -- /dev/ttyUSB0         # Linux (Serial)
dotnet run -- pcsc                 # PC/SC (USB CCID)
```

---
> 전체 SDK: [README](../../README.md) · MIT License
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/14-SystemFeatures`
