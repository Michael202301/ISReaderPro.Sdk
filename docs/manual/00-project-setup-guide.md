# 예제 프로젝트 생성 및 실행 가이드

이 문서는 **Iksung.Reader SDK** 예제를 처음 접하는 개발자가  
**직접 프로젝트를 만들거나 제공된 예제를 실행**할 수 있도록 단계별로 안내합니다.

---

## 목차

1. [사전 준비](#1-사전-준비)
2. [예제 소스코드 다운로드](#2-예제-소스코드-다운로드)
3. [제공된 예제 열고 실행하기](#3-제공된-예제-열고-실행하기)
4. [신규 NET 8 프로젝트 만들기 — Visual Studio](#4-신규-net-8-프로젝트-만들기--visual-studio-2022)
5. [신규 NET 8 프로젝트 만들기 — CLI](#5-신규-net-8-프로젝트-만들기--net-cli)
6. [신규 NET Framework 4.7.2 프로젝트 만들기](#6-신규-net-framework-472-프로젝트-만들기)
7. [SDK 패키지 추가하기](#7-sdk-패키지-추가하기)
8. [리더기 포트 확인 방법](#8-리더기-포트-확인-방법)
9. [첫 번째 코드 작성 및 실행](#9-첫-번째-코드-작성-및-실행)
10. [자주 묻는 질문 및 오류 해결](#10-자주-묻는-질문-및-오류-해결)

---

## 1. 사전 준비

### 1-1. 필수 소프트웨어

아래 소프트웨어가 PC에 설치되어 있어야 합니다.

| 소프트웨어 | 버전 | 다운로드 |
|-----------|------|---------|
| Visual Studio 2022 | 17.x 이상 | https://visualstudio.microsoft.com |
| .NET SDK | 8.0 이상 | https://dotnet.microsoft.com/download |
| Git | 최신 | https://git-scm.com |

> **Visual Studio 설치 시 워크로드 선택:**  
> `.NET 데스크톱 개발` 워크로드를 반드시 체크합니다.  
> (NET Framework 4.7.2 예제 빌드에 필요)

---

> 📷 **[이미지 자리]**  
> _Visual Studio Installer 화면 — ".NET 데스크톱 개발" 워크로드가 체크된 상태_

---

### 1-2. .NET SDK 설치 확인

터미널(명령 프롬프트 또는 PowerShell)을 열고 아래 명령을 실행합니다.

```bash
dotnet --version
```

출력 예시:
```
8.0.xxx
```

버전이 `8.0` 이상이면 준비 완료입니다.

---

### 1-3. 하드웨어 연결

리더기를 USB 케이블 또는 RS-232 케이블로 PC에 연결합니다.

---

> 📷 **[이미지 자리]**  
> _IS-NFC 리더기를 USB 케이블로 PC에 연결한 사진_

---

연결 후 **Windows 장치 관리자**에서 포트 번호를 확인합니다 ([8장 참조](#8-리더기-포트-확인-방법)).

---

## 2. 예제 소스코드 다운로드

### 2-1. GitHub에서 클론

터미널을 열고 원하는 폴더로 이동한 뒤 아래 명령을 실행합니다.

```bash
git clone https://github.com/Michael202301/ISReaderPro.Sdk.git
```

클론이 완료되면 `ISReaderPro.Sdk` 폴더가 생성됩니다.

```
ISReaderPro.Sdk/
├── NET8-Samples/          ← .NET 8 예제 (14개)
│   ├── HelloWorld.Console/
│   ├── 01-ReadAnyUid/
│   ├── 02-Iso14443a/
│   └── ...
├── NET4x-Samples/         ← .NET Framework 4.7.2 예제 (14개)
│   ├── HelloWorld.Console/
│   ├── 01-ReadAnyUid/
│   └── ...
├── src/
│   └── Iksung.Reader/     ← SDK 소스 (빌드 대상)
└── docs/                  ← 이 문서 포함
```

### 2-2. ZIP으로 다운로드 (Git 미설치 시)

GitHub 저장소 페이지에서 **Code → Download ZIP** 버튼을 클릭하여  
ZIP 파일을 받은 뒤 원하는 폴더에 압축을 해제합니다.

---

> 📷 **[이미지 자리]**  
> _GitHub 저장소 페이지에서 "Code" 초록 버튼 클릭 → "Download ZIP" 메뉴가 보이는 화면_

---

## 3. 제공된 예제 열고 실행하기

### 3-1. Visual Studio 2022로 솔루션 열기

1. Visual Studio 2022를 실행합니다.
2. 시작 화면에서 **"프로젝트 또는 솔루션 열기"** 를 클릭합니다.

---

> 📷 **[이미지 자리]**  
> _Visual Studio 2022 시작 화면 — "프로젝트 또는 솔루션 열기" 버튼이 강조된 모습_

---

3. 파일 탐색기에서 클론한 폴더로 이동하여  
   `ISReaderPro.Sdk.slnx` 파일을 선택하고 **열기**를 클릭합니다.

---

> 📷 **[이미지 자리]**  
> _파일 탐색기에서 `ISReaderPro.Sdk.slnx` 파일을 선택한 화면_

---

4. 솔루션이 열리면 **솔루션 탐색기**에 두 폴더가 보입니다.

---

> 📷 **[이미지 자리]**  
> _Visual Studio 솔루션 탐색기 — `NET8-Samples`와 `NET4x-Samples` 폴더 트리가 펼쳐진 모습_

---

### 3-2. 실행할 예제 프로젝트 선택

1. 솔루션 탐색기에서 실행하려는 예제 프로젝트를 **우클릭**합니다.  
   (예: `NET8-Samples` → `01-ReadAnyUid`)
2. **"시작 프로젝트로 설정"** 을 클릭합니다.

---

> 📷 **[이미지 자리]**  
> _솔루션 탐색기에서 `ReadAnyUid` 프로젝트를 우클릭 → "시작 프로젝트로 설정" 메뉴가 보이는 화면_

---

### 3-3. 포트 인수 설정 후 실행

예제는 실행 시 COM 포트를 인수로 받습니다.  
Visual Studio에서 인수를 설정하려면:

1. 프로젝트를 우클릭 → **"속성"** 클릭
2. **"디버그"** 탭 → **"일반"** → **"명령줄 인수"** 에 포트 번호 입력  
   예: `COM3`

---

> 📷 **[이미지 자리]**  
> _프로젝트 속성 창 — 디버그 탭의 "명령줄 인수" 입력란에 `COM3` 이 입력된 화면_

---

3. **F5** 키 또는 상단 **▶ 실행** 버튼을 눌러 실행합니다.

---

> 📷 **[이미지 자리]**  
> _콘솔 창에 `[IKSUNG] Firmware: V1.x.x` 와 UID 출력이 보이는 실행 결과 화면_

---

### 3-4. dotnet CLI로 바로 실행 (Visual Studio 없이)

터미널에서 클론한 폴더로 이동한 뒤 아래 명령을 실행합니다.

```bash
# .NET 8 예제 실행 (Windows COM3 기준)
dotnet run --project NET8-Samples/01-ReadAnyUid -- COM3

# .NET 8 예제 실행 (Linux)
dotnet run --project NET8-Samples/01-ReadAnyUid -- /dev/ttyUSB0

# .NET Framework 4.7.2 예제 실행
dotnet run --project NET4x-Samples/01-ReadAnyUid -- COM3
```

> `--` 뒤의 값이 프로그램에 전달되는 인수입니다 (포트 이름).

---

## 4. 신규 NET 8 프로젝트 만들기 — Visual Studio 2022

SDK를 이용해 처음부터 직접 프로젝트를 만드는 방법입니다.

### 4-1. 새 프로젝트 만들기

1. Visual Studio 2022를 실행합니다.
2. **"새 프로젝트 만들기"** 를 클릭합니다.

---

> 📷 **[이미지 자리]**  
> _Visual Studio 2022 시작 화면 — "새 프로젝트 만들기" 버튼_

---

3. 검색창에 `콘솔` 을 입력하고  
   **"콘솔 앱"** (C#, .NET / .NET Core용) 을 선택합니다.  
   ⚠️ **"콘솔 앱 (.NET Framework)"** 와 다릅니다. 올바른 항목을 선택하세요.

---

> 📷 **[이미지 자리]**  
> _새 프로젝트 대화 상자 — 검색창에 "콘솔" 입력 후 C# "콘솔 앱"(비 .NET Framework)이 선택된 화면_

---

4. **"다음"** 클릭 후 프로젝트 이름과 저장 위치를 설정합니다.

| 항목 | 예시 값 |
|------|--------|
| 프로젝트 이름 | `MyNfcApp` |
| 위치 | `C:\Projects\` |
| 솔루션 이름 | `MyNfcApp` |

5. **"다음"** 클릭 후 프레임워크를 선택합니다.

---

> 📷 **[이미지 자리]**  
> _프레임워크 선택 화면 — `.NET 8.0 (장기 지원)` 이 선택된 드롭다운_

---

6. **"만들기"** 를 클릭합니다.

### 4-2. SDK 패키지 추가

[7장 SDK 패키지 추가하기](#7-sdk-패키지-추가하기)를 참조하세요.

### 4-3. Program.cs 코드 작성

생성된 `Program.cs` 파일을 열고 내용을 아래와 같이 작성합니다.

```csharp
using Iksung.Reader;
using Iksung.Reader.Exceptions;

string portName = args.Length > 0 ? args[0] : "COM3";

Console.WriteLine($"[IKSUNG] Connecting to {portName}...");

await using var reader = await IksungReader.ConnectSerialAsync(portName);
Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");
Console.WriteLine("[IKSUNG] Place a card. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}  UID: {BitConverter.ToString(uid).Replace("-", "")}");
    }
    catch (IksungTimeoutException)    { /* 카드 없음 — 대기 */ }
    catch (IksungProtocolException)   { /* 인식 실패 — 대기 */ }
    catch (OperationCanceledException){ break; }

    try { await Task.Delay(300, cts.Token); }
    catch (OperationCanceledException){ break; }
}

Console.WriteLine("\n[IKSUNG] Done.");
```

---

## 5. 신규 NET 8 프로젝트 만들기 — .NET CLI

Visual Studio 없이 터미널만으로 프로젝트를 생성하는 방법입니다.

### 5-1. 프로젝트 생성

```bash
# 프로젝트 폴더 생성 및 이동
mkdir MyNfcApp
cd MyNfcApp

# 콘솔 앱 생성 (.NET 8)
dotnet new console -f net8.0
```

생성 결과:
```
MyNfcApp/
├── MyNfcApp.csproj
└── Program.cs
```

### 5-2. SDK 패키지 추가

```bash
dotnet add package Iksung.Reader
```

> 패키지가 NuGet에 아직 게시되지 않은 경우 [7-2장 로컬 참조](#7-2-로컬-프로젝트-참조-현재-개발-중-권장) 방법을 사용합니다.

### 5-3. 코드 작성 및 실행

```bash
# Program.cs 편집 (원하는 에디터 사용)
code Program.cs       # VS Code
notepad Program.cs    # 메모장

# 실행
dotnet run -- COM3
```

---

## 6. 신규 NET Framework 4.7.2 프로젝트 만들기

레거시 WinForms / WPF 앱에 SDK를 통합하거나  
.NET Framework 환경에서 예제를 실행하려는 경우 이 방법을 사용합니다.

### 6-1. Visual Studio에서 신규 생성

1. **"새 프로젝트 만들기"** 클릭
2. 검색창에 `콘솔` 입력 →  
   **"콘솔 앱 (.NET Framework)"** (C#) 선택

---

> 📷 **[이미지 자리]**  
> _새 프로젝트 대화 상자 — "콘솔 앱 (.NET Framework)" 항목이 선택된 화면_

---

3. **"다음"** 클릭 후 이름·위치 설정
4. 프레임워크 드롭다운에서 **`.NET Framework 4.7.2`** 선택

---

> 📷 **[이미지 자리]**  
> _프레임워크 드롭다운 — ".NET Framework 4.7.2" 가 선택된 화면_

---

5. **"만들기"** 클릭

### 6-2. .csproj 파일 설정 확인

생성된 `.csproj` 파일을 열어 아래 항목을 확인·추가합니다.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>      <!-- C# 최신 문법 활성화 -->
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>  <!-- using 명시 필요 -->
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Iksung.Reader" Version="0.*" />
  </ItemGroup>
</Project>
```

> `LangVersion=latest` 를 설정하면 .NET Framework 4.7.2에서도  
> C# 12 문법(컬렉션 표현식, switch 식 등)을 사용할 수 있습니다.

### 6-3. Program.cs 코드 작성 (.NET 4.x 스타일)

.NET Framework 4.x에서는 **Top-level statements**를 사용할 수 없습니다.  
반드시 클래스와 `Main` 메서드를 명시해야 합니다.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;

namespace MyNfcApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string portName = args.Length > 0 ? args[0] : "COM3";

            Console.WriteLine($"[IKSUNG] Connecting to {portName}...");

            var reader = await IksungReader.ConnectSerialAsync(portName);
            try
            {
                Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");
                Console.WriteLine("[IKSUNG] Place a card. Press Ctrl+C to exit.\n");

                using (var cts = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
                            Console.WriteLine(
                                $"{DateTime.Now:HH:mm:ss}  UID: " +
                                $"{BitConverter.ToString(uid).Replace("-", "")}");
                        }
                        catch (IksungTimeoutException)    { }
                        catch (IksungProtocolException)   { }
                        catch (OperationCanceledException){ break; }

                        try { await Task.Delay(300, cts.Token); }
                        catch (OperationCanceledException){ break; }
                    }
                }

                Console.WriteLine("\n[IKSUNG] Done.");
            }
            finally
            {
                await reader.DisposeAsync();   // await using 대신 명시적 해제
            }
        }
    }
}
```

> .NET 4.x와 .NET 8의 문법 차이 전체 목록은  
> [.NET Framework 4.x 가이드](net4x-guide.md)를 참조하세요.

---

## 7. SDK 패키지 추가하기

### 7-1. NuGet 패키지로 추가 (게시 후 권장)

#### Visual Studio — NuGet 패키지 관리자 사용

1. 솔루션 탐색기에서 프로젝트를 **우클릭**
2. **"NuGet 패키지 관리"** 클릭

---

> 📷 **[이미지 자리]**  
> _솔루션 탐색기에서 프로젝트 우클릭 → "NuGet 패키지 관리" 메뉴가 표시된 화면_

---

3. **"찾아보기"** 탭에서 `Iksung.Reader` 검색

---

> 📷 **[이미지 자리]**  
> _NuGet 패키지 관리자 — 검색창에 "Iksung.Reader" 입력 후 패키지 목록에 결과가 나온 화면_

---

4. `Iksung.Reader` 선택 → 오른쪽 **"설치"** 버튼 클릭

---

> 📷 **[이미지 자리]**  
> _NuGet 패키지 관리자 — Iksung.Reader 패키지를 선택하고 "설치" 버튼이 보이는 화면_

---

#### dotnet CLI 사용

```bash
dotnet add package Iksung.Reader
```

### 7-2. 로컬 프로젝트 참조 (현재 개발 중 권장)

NuGet에 아직 게시되지 않은 경우, SDK 소스를 직접 참조합니다.

#### Visual Studio — 프로젝트 참조 추가

1. 솔루션 탐색기에서 내 프로젝트 우클릭
2. **"추가"** → **"프로젝트 참조"** 클릭

---

> 📷 **[이미지 자리]**  
> _솔루션 탐색기 우클릭 메뉴 — "추가 → 프로젝트 참조" 항목이 보이는 화면_

---

3. **"찾아보기"** 탭 → SDK 폴더로 이동 →  
   `src/Iksung.Reader/Iksung.Reader.csproj` 선택 → **"확인"**

---

> 📷 **[이미지 자리]**  
> _참조 관리자 대화 상자 — `Iksung.Reader.csproj` 파일이 선택된 상태_

---

#### `.csproj` 파일 직접 편집

`.csproj` 파일을 텍스트 에디터로 열어 아래 내용을 추가합니다.  
(`../../` 부분은 SDK 소스 폴더와의 **상대 경로**에 맞게 조정하세요.)

```xml
<ItemGroup>
  <!-- SDK 소스가 상위 폴더에 있는 경우 -->
  <ProjectReference Include="..\..\src\Iksung.Reader\Iksung.Reader.csproj" />
</ItemGroup>
```

#### dotnet CLI — 참조 추가

```bash
dotnet add reference ../../src/Iksung.Reader/Iksung.Reader.csproj
```

---

## 8. 리더기 포트 확인 방법

### 8-1. Windows — 장치 관리자

1. `Win + X` → **"장치 관리자"** 클릭  
   또는 검색창에 `장치 관리자` 입력 후 실행

---

> 📷 **[이미지 자리]**  
> _Windows 장치 관리자 창 — "포트(COM 및 LPT)" 섹션이 펼쳐져 있고,  
> `USB Serial Port (COM3)` 같은 항목이 보이는 화면_

---

2. **"포트 (COM 및 LPT)"** 섹션을 펼칩니다.
3. 리더기가 연결된 항목에서 포트 번호를 확인합니다.  
   예: `USB Serial Port (COM3)` → 포트 이름: **`COM3`**

> 리더기를 뽑았다 꽂으면 목록이 사라졌다가 다시 나타나는 항목이 리더기입니다.

### 8-2. Linux

```bash
# 연결된 시리얼 장치 목록 확인
ls /dev/ttyUSB* /dev/ttyACM* 2>/dev/null

# 또는 dmesg로 최근 연결 장치 확인
dmesg | grep tty | tail -5
```

출력 예시:
```
/dev/ttyUSB0
```

포트 접근 권한이 없으면 아래 명령으로 사용자를 `dialout` 그룹에 추가합니다.

```bash
sudo usermod -a -G dialout $USER
# 로그아웃 후 재로그인 필요
```

### 8-3. macOS

```bash
ls /dev/cu.usbserial-*
```

출력 예시:
```
/dev/cu.usbserial-1234
```

---

## 9. 첫 번째 코드 작성 및 실행

### 9-1. 빌드

#### Visual Studio
**빌드 → 솔루션 빌드** (`Ctrl + Shift + B`)

---

> 📷 **[이미지 자리]**  
> _Visual Studio 출력 창 — "빌드: 1 성공, 0 실패, 0 경고" 메시지가 보이는 화면_

---

#### dotnet CLI

```bash
dotnet build
```

성공 출력 예시:
```
빌드했습니다.
    경고 0개
    오류 0개
```

### 9-2. 실행

#### Visual Studio에서 실행

- **F5** : 디버그 모드로 실행 (중단점 사용 가능)
- **Ctrl + F5** : 디버그 없이 실행 (더 빠름)

#### dotnet CLI에서 실행

```bash
# Windows
dotnet run -- COM3

# Linux
dotnet run -- /dev/ttyUSB0

# macOS
dotnet run -- /dev/cu.usbserial-1234
```

### 9-3. 정상 실행 결과

리더기가 올바르게 연결된 경우 아래와 같이 출력됩니다.

```
[IKSUNG] Connecting to COM3...
[IKSUNG] Firmware: V1.2.3
[IKSUNG] Place a card. Press Ctrl+C to exit.

```

이후 카드를 리더기 위에 올려놓으면:

```
14:23:05  UID: 04A3F7C2
14:23:08  UID: 04A3F7C2
```

---

> 📷 **[이미지 자리]**  
> _콘솔 창에 펌웨어 버전과 UID가 출력된 실행 결과 화면_

---

**Ctrl + C** 를 누르면 프로그램이 종료됩니다.

```
^C
[IKSUNG] Done.
```

---

## 10. 자주 묻는 질문 및 오류 해결

### ❌ `'IksungReader' does not exist in the current context`

**원인**: SDK 패키지가 설치되지 않았거나 `using Iksung.Reader;` 가 누락되었습니다.

**해결**:
1. NuGet 패키지 설치 확인 ([7장](#7-sdk-패키지-추가하기))
2. 파일 상단에 `using Iksung.Reader;` 추가
3. `dotnet restore` 실행 후 재빌드

---

### ❌ `'await' requires that the method be marked async`

**원인**: .NET Framework 4.x에서 `Main` 메서드에 `async`가 없습니다.

**해결**: `Main` 선언을 아래와 같이 수정합니다.

```csharp
// ❌ 잘못된 예
static void Main(string[] args) { ... }

// ✅ 올바른 예
static async Task Main(string[] args) { ... }
```

---

### ❌ `System.IO.IOException: The port 'COM3' does not exist`

**원인**: 지정한 포트 이름이 잘못되었거나 리더기가 연결되어 있지 않습니다.

**해결**:
1. 리더기 USB 케이블이 완전히 꽂혀 있는지 확인
2. [8장](#8-리더기-포트-확인-방법)을 참조해 정확한 포트 이름 재확인
3. 다른 프로그램이 해당 포트를 사용 중인지 확인 (다른 터미널·시리얼 모니터 종료)

---

### ❌ `Access to the port 'COM3' is denied`

**원인**: 다른 프로세스가 포트를 이미 점유 중이거나 권한 부족입니다.

**해결**:
- 다른 시리얼 통신 프로그램(터미널, ISReaderPro 등)을 모두 닫은 뒤 재시도
- Linux의 경우 `dialout` 그룹 추가 후 재로그인

---

### ❌ `IksungTimeoutException` 이 계속 발생하고 UID가 읽히지 않음

**원인**: 카드가 리더기 인식 범위 밖에 있거나 카드 타입이 맞지 않습니다.

**해결**:
- 카드를 리더기 중앙에 완전히 밀착시켜 올려놓기
- `ReadIso14443aUidAsync` 대신 `ReadIso14443bUidAsync` 또는  
  `ReadIso15693UidAsync` 시도 (카드 타입에 따라 다름)
- [01-ReadAnyUid 예제](../NET8-Samples/01-ReadAnyUid) 로 모든 타입을 순차 시도

---

### ❌ .NET 8 예제를 .NET Framework 환경에서 빌드하면 오류 발생

**원인**: `NET8-Samples` 폴더의 예제는 `net8.0` 대상입니다.

**해결**: `NET4x-Samples` 폴더의 해당 예제를 사용합니다.

| .NET 8 예제 | 대응하는 .NET 4.x 예제 |
|------------|----------------------|
| `NET8-Samples/01-ReadAnyUid` | `NET4x-Samples/01-ReadAnyUid` |
| `NET8-Samples/03-MifareClassic` | `NET4x-Samples/03-MifareClassic` |
| `NET8-Samples/06-AutoRead` | `NET4x-Samples/06-AutoRead` |
| (동일한 번호로 대응) | … |

---

### ❌ `ImplicitUsings` 관련 빌드 오류 (.NET Framework)

**원인**: .NET Framework 4.x 프로젝트에서 `ImplicitUsings=enable` 이 설정된 경우.

**해결**: `.csproj` 에서 비활성화합니다.

```xml
<ImplicitUsings>disable</ImplicitUsings>
```

이후 파일 상단에 필요한 `using` 을 모두 명시합니다.

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Iksung.Reader;
using Iksung.Reader.Exceptions;
```

---

## 관련 문서

| 문서 | 내용 |
|------|------|
| [01. 시작하기](01-getting-started.md) | SDK 설치, 연결, Hello World |
| [.NET 4.x 가이드](net4x-guide.md) | WinForms/WPF 통합, 문법 차이 |
| [API 레퍼런스](api-reference.md) | 전체 공개 메서드 목록 |
| [13. 예외 처리](13-error-handling.md) | 예외 종류 및 처리 패턴 |

---

[← 목차](README.md) | [다음: 시작하기 →](01-getting-started.md)

---

© 익성전자 (Iksung Electronics). MIT License.
