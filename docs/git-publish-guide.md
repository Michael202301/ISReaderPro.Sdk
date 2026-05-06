# Git 공개 / 비공개 가이드

이 문서는 `ISReaderPro.Sdk` 리포지터리를 GitHub에 공개할 때  
**올려야 할 파일**과 **올리면 안 되는 파일**을 정리합니다.

---

## 1. 공개해야 할 파일 ✅

| 경로 | 이유 |
|------|------|
| `src/Iksung.Reader/**` | SDK 핵심 소스 코드 — 공개 SDK의 가치 그 자체 |
| `tests/Iksung.Reader.Tests/**` | 품질 신뢰도 확보, 기여자 참여 유도 |
| `samples/**` | 고객이 바로 실행해볼 수 있는 예제 |
| `docs/manual/**` | API 사용법 설명서 |
| `ISReaderPro.Sdk.slnx` | 솔루션 파일 — Visual Studio에서 열기 위해 필요 |
| `README.md` | 프로젝트 소개, 설치 방법 |
| `LICENSE` | MIT 라이선스 명시 (공개 배포 필수) |
| `.gitignore` | 빌드 산출물 제외 규칙 |
| `CHANGELOG.md` | 버전별 변경 이력 (있는 경우) |
| `CONTRIBUTING.md` | 기여 가이드 (있는 경우) |

---

## 2. 공개하면 안 되는 파일 🚫

### 2-1. 빌드 산출물 (`.gitignore`에 이미 포함)

| 경로 | 이유 |
|------|------|
| `**/bin/` | 컴파일 결과물 — 소스에서 재생성 가능 |
| `**/obj/` | 임시 빌드 파일 |
| `**/*.nupkg` | NuGet 패키지 파일 — NuGet.org에 별도 업로드 |
| `**/publish/` | 배포용 바이너리 |

### 2-2. 개발 환경 파일 (`.gitignore`에 이미 포함)

| 경로 | 이유 |
|------|------|
| `.vs/` | Visual Studio 로컬 설정 |
| `*.user` | 개발자 개인 설정 |
| `.vscode/` | VS Code 로컬 설정 (launch.json 제외 가능) |
| `.idea/` | JetBrains Rider 설정 |

### 2-3. ⚠️ 절대 올리면 안 되는 파일 (중요)

| 경로/유형 | 이유 |
|-----------|------|
| **펌웨어 바이너리** (`*.hex`, `*.bin`, `*.elf`) | 지식재산권, 역공학 리스크 |
| **펌웨어 소스 코드** | 핵심 기술 유출 |
| **내부 프로토콜 상세 문서** (`PROTOCOL.md` 내 미공개 섹션) | 경쟁사에 의한 복제 리스크 |
| **고객사 전용 키/설정** | 고객 보안 데이터 |
| **DESFire 암호화 키** | 카드 복제 위험 |
| **API 키, NuGet 배포 키** | 계정 탈취 위험 |
| **`.env`, `*.secret`, `appsettings.Production.json`** | 비밀 값 포함 가능 |
| **테스트용 실제 카드 데이터** | 개인정보 또는 보안 데이터 |
| **고객사 납품 계약서, 견적서** | 영업 기밀 |
| **ISReaderPro-V6.01 (WPF 앱 소스)** | 내부 전용 앱, 별도 비공개 리포 유지 |

---

## 3. 리포지터리 구조 권고

```
iksung-reader-sdk/           ← 이 리포 (공개 GitHub)
├── .gitignore
├── LICENSE                  ← MIT
├── README.md
├── ISReaderPro.Sdk.slnx
├── src/
│   └── Iksung.Reader/       ← SDK 소스 ✅ 공개
├── tests/
│   └── Iksung.Reader.Tests/ ← 단위 테스트 ✅ 공개
├── samples/
│   ├── 01-ReadAnyUid/       ← 예제 ✅ 공개
│   ├── ...
│   └── net4x/               ← .NET 4.x 예제 ✅ 공개
└── docs/
    └── manual/              ← 사용 설명서 ✅ 공개

iksung-reader-internal/      ← 별도 비공개 리포 🚫
├── ISReaderPro-V6.01/       ← WPF 앱 (내부 전용)
├── firmware/                ← 펌웨어 소스
└── docs/internal/           ← 내부 프로토콜 전체 문서
```

---

## 4. `PROTOCOL.md` 공개 범위 판단 기준

프로토콜 문서는 **SDK를 사용하는 데 필요한 내용만** 공개하고,  
**펌웨어 구현 세부 사항**은 공개하지 않습니다.

| 내용 | 공개 여부 |
|------|----------|
| 명령어 코드 (CMD1, CMD2 값) | ✅ 공개 — SDK 사용에 필요 |
| 요청/응답 패킷 구조 | ✅ 공개 — Raw 명령 사용에 필요 |
| 오류 코드 (State byte 의미) | ✅ 공개 — 오류 처리에 필요 |
| 펌웨어 내부 동작 구현 알고리즘 | 🚫 비공개 |
| 미출시 명령어 코드 | 🚫 비공개 |
| 보드 회로도, 하드웨어 스펙 | 🚫 비공개 |

---

## 5. 최초 공개 전 체크리스트

```
□ .gitignore 확인 — bin/, obj/, .vs/ 포함 여부
□ git log 전체 확인 — 과거 커밋에 민감 정보가 없는지
□ README.md 작성 — 제품 소개, 설치 방법, 라이선스
□ LICENSE 파일 추가 — MIT
□ CHANGELOG.md 작성 — v0.1.0-preview 항목
□ 펌웨어 바이너리 없는지 확인 (*.hex, *.bin, *.elf)
□ API 키, NuGet 키 없는지 확인
□ ISReaderPro-V6.01 경로 포함 안 됨을 확인
□ 내부 고객 데이터 없는지 확인
□ dotnet build — 0 errors, 0 warnings 확인
□ dotnet test  — 전체 통과 확인
```

---

## 6. 민감 정보가 이미 커밋된 경우

```bash
# git history에서 특정 파일 완전 삭제 (BFG 권장)
# BFG Repo-Cleaner 설치 후:
bfg --delete-files "*.hex"
bfg --delete-files "secrets.json"
git reflog expire --expire=now --all
git gc --prune=now --aggressive
git push origin --force --all

# 또는 git filter-repo 사용:
git filter-repo --path secrets.json --invert-paths
```

> **주의:** force-push 전에 팀원에게 알리고, 브랜치를 re-clone해야 합니다.

---

## 7. NuGet 배포 키 관리

```
NuGet API 키는 절대 소스 코드에 포함하지 말 것.

권장:
  - GitHub Actions Secret → ${{ secrets.NUGET_API_KEY }}
  - 로컬 배포 시 환경변수로 전달:
      $env:NUGET_API_KEY = "..."
      dotnet nuget push *.nupkg --api-key $env:NUGET_API_KEY
```

---

*이 문서는 `ISReaderPro.Sdk` 공개 리포지터리 기준입니다.  
`ISReaderPro-V6.01` (WPF 앱)은 별도 비공개 리포에서 관리합니다.*
