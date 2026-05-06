# .NET Framework 4.x 사용 가이드

Iksung.Reader SDK는 **.NET 8** 및 **.NET Framework 4.7.2** 를 모두 지원합니다.  
이 문서는 .NET 4.x 환경에서 달라지는 사용 방법을 설명합니다.

---

## 지원 버전

| 플랫폼 | 최소 버전 | 용도 |
|--------|----------|------|
| .NET 8 | 8.0 | 신규 개발 권장 |
| .NET Framework | 4.7.2 | 기존 WinForms/WPF 앱 통합 |

---

## 주요 문법 차이

### 1. 클래스·메서드 선언 필수 (Top-level statements 미지원)

**.NET 8 예제:**
```csharp
// Program.cs — 클래스 없이 바로 코드 작성 가능
using Iksung.Reader;
var reader = await IksungReader.ConnectSerialAsync("COM3");
```

**.NET 4.x 예제:**
```csharp
using System;
using System.Threading.Tasks;
using Iksung.Reader;

namespace MyApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var reader = await IksungReader.ConnectSerialAsync("COM3");
        }
    }
}
```

---

### 2. `await using` → `try/finally + DisposeAsync()`

**.NET 8:**
```csharp
await using var reader = await IksungReader.ConnectSerialAsync("COM3");
// 블록 종료 시 자동 해제
```

**.NET 4.x:**
```csharp
var reader = await IksungReader.ConnectSerialAsync("COM3");
try
{
    // ... 작업 수행 ...
}
finally
{
    await reader.DisposeAsync(); // 명시적으로 해제
}
```

---

### 3. `using` 지시문 명시 필요

**.NET 8** 프로젝트는 `<ImplicitUsings>enable</ImplicitUsings>` 설정으로  
`System`, `System.Threading.Tasks` 등이 자동으로 포함됩니다.

**.NET 4.x** 프로젝트는 필요한 `using`을 모두 명시해야 합니다:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;
```

---

### 4. 컬렉션 표현식 — 선택 사항

`LangVersion=latest`를 설정하면 .NET 4.x에서도 C# 12 문법을 사용할 수 있습니다.

```xml
<!-- .csproj -->
<LangVersion>latest</LangVersion>
```

이 경우 아래 두 표현 모두 사용 가능합니다:

```csharp
// C# 12 스타일 (LangVersion=latest 설정 시 net472에서도 가능)
byte[] key = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

// 전통적인 스타일 (항상 가능)
byte[] key = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
```

---

### 5. 이벤트 핸들러 등록 / 해제 명시

**.NET 8** 예제에서는 람다를 사용하는 경우가 많지만,  
.NET 4.x에서는 해제를 위해 **이름 있는 메서드**를 권장합니다.

```csharp
// ✅ 권장 — 해제 가능
EventHandler<TagDetectedEventArgs> handler = OnTagDetected;
reader.TagDetected += handler;
// ... 사용 후 ...
reader.TagDetected -= handler;

// ⚠️ 비권장 — 익명 람다는 해제 불가
reader.TagDetected += (s, e) => Console.WriteLine(e.UidHex);
```

---

### 6. WinForms — UI 스레드 마샬링 (필수)

`TagDetected` 이벤트는 **백그라운드 스레드**에서 발생합니다.  
WinForms 컨트롤은 **UI 스레드에서만** 수정할 수 있습니다.

```csharp
private void OnTagDetected(object? sender, TagDetectedEventArgs e)
{
    if (InvokeRequired)
    {
        // 백그라운드 스레드 → UI 스레드로 전환
        Invoke(new Action<TagDetectedEventArgs>(UpdateLabel), e);
        return;
    }
    UpdateLabel(e);
}

private void UpdateLabel(TagDetectedEventArgs e)
{
    lblUid.Text = "UID: " + e.UidHex;
}
```

---

### 7. WPF — Dispatcher 마샬링

```csharp
private void OnTagDetected(object? sender, TagDetectedEventArgs e)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        lblUid.Content = "UID: " + e.UidHex;
    });
}
```

---

## .NET 4.x 전용 예제 목록

| 예제 | 경로 | 설명 |
|------|------|------|
| 01-HelloWorld | `samples/net4x/01-HelloWorld/` | 연결, UID 폴링 루프 |
| 02-MifareClassic | `samples/net4x/02-MifareClassic/` | 인증, 블록 읽기/쓰기 |
| 03-AutoRead | `samples/net4x/03-AutoRead/` | 이벤트 기반 카드 감지 |
| 04-WinFormsIntegration | `samples/net4x/04-WinFormsIntegration/` | WinForms Invoke 패턴 |

```bash
# 빌드
dotnet build samples/net4x/01-HelloWorld/HelloWorld.Net4x.csproj

# 실행
dotnet run --project samples/net4x/01-HelloWorld/HelloWorld.Net4x.csproj -- COM3
```

---

## 프로젝트 설정 참조 (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>   <!-- C# 12 문법 활성화 -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>  <!-- using 명시 필요 -->
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Iksung.Reader" Version="0.*" />
  </ItemGroup>
</Project>
```

---

[← 목차](README.md)
