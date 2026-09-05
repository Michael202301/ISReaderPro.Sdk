# Sample 15 – PC/SC (CCID) Channel

PC/SC(CCID) 채널로 IS 시리즈 리더기에 연결해 **리더 목록 조회 · 자동 선택 · 연결 · 버전 · 카드 UID · 버저 · RF On/Off · AutoRead**를 시연합니다.

> ⚠ PC/SC는 **Windows 전용**입니다. Smart Card Service(SCardSvr)가 실행 중이어야 하며, 이 예제는 `net8.0-windows`로 빌드됩니다(winscard.dll).

## 실행
```bash
dotnet run                  # 첫 번째 감지 리더에 자동 연결
dotnet run -- "리더이름"     # 특정 PC/SC 리더 지정
```

> 크로스플랫폼(Serial/TCP)으로 쓰려면 `01-ReadAnyUid` 등 다른 예제를 참고하세요.

---
> 전체 SDK: [README](../../README.md) · MIT License
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/15-PcscChannel`
