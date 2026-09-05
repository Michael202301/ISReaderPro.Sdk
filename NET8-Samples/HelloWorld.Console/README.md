# HelloWorld.Console — 최소 시작 예제

리더에 연결해 펌웨어 버전과 카드 UID를 읽는 가장 단순한 예제입니다.

## 실행
```bash
dotnet run -- COM3                 # Windows (Serial)
dotnet run -- /dev/ttyUSB0         # Linux (Serial)
dotnet run -- pcsc                 # PC/SC (USB CCID)
```

---
> 전체 SDK: [README](../../README.md) · MIT License
> .NET Framework 4.7.2 버전: `../../NET4x-Samples/HelloWorld.Console`
