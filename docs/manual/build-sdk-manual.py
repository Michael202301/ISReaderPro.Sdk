# -*- coding: utf-8 -*-
"""
Iksung.Reader SDK — 통합 매뉴얼 빌더 (.md → .md 통합 + .html)

사용법:
    python build-sdk-manual.py

출력:
    Iksung_Reader_SDK_Manual.md    — 16 챕터 + 부록 합본
    Iksung_Reader_SDK_Manual.html  — A4 인쇄용 HTML (PDF 빌드 입력)

이후 PDF 생성:
    node build-sdk-manual-pdf.js   — puppeteer 로 A4 PDF (헤더/푸터/페이지번호 자동)

의존성: pip install markdown
"""
import os, sys, io, re
from datetime import date

import markdown

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))

# ── 챕터 순서 ────────────────────────────────────────────────
CHAPTERS = [
    '00-project-setup-guide.md',
    '01-getting-started.md',
    '02-common-commands.md',
    '03-iso14443ab.md',
    '04-mifare-classic.md',
    '05-mifare-ultralight-ntag.md',
    '06-iso15693.md',
    '07-desfire.md',
    '08-lf125khz.md',
    '09-autoread.md',
    '10-iso7816-usim.md',
    '11-bluetooth.md',
    '12-relay.md',
    '13-error-handling.md',
    '14-system-features.md',
    '15-pcsc-channel.md',
    'api-reference.md',
    'net4x-guide.md',
]

# ── 통합본 프롤로그 ──────────────────────────────────────────
PROLOG = """# Iksung.Reader SDK — 통합 사용 설명서

**버전:** SDK V1.0 / .NET 8 + .NET Framework 4.7.2 지원
**문서 빌드일:** {build_date}
**대상:** 익성전자 NFC/RFID 리더기 응용 프로그램 개발자
**채널 지원:** Serial (UART 115200 bps) · USB CCID (PC/SC) · TCP/IP Socket

---

## 머리말

본 문서는 **Iksung.Reader .NET SDK** 의 통합 사용 설명서입니다.
모든 카드 표준 / 시스템 기능 / 예외 처리 / API 레퍼런스를 단일 문서로 정리했으며,
이전 `IS_3400_V3.0_Reader-C#_Library_V3.2.pdf` (D2xx 기반 단종 라이브러리) 의 후속 문서입니다.

| 항목 | 이전 (V3.2 / D2xx) | 신규 (Iksung.Reader SDK) |
|------|---------------------------|--------------------------|
| 인터페이스 | FTDI D2xx 만 지원 | **Serial / TCP Socket / PC/SC** 3개 채널 추상화 |
| 비동기 | 동기 호출 + 콜백 | **`async/await` 전체 지원** |
| 패키지 | 직접 DLL 참조 | **NuGet `Iksung.Reader`** |
| 플랫폼 | Windows .NET Framework 만 | **.NET 8 (크로스 플랫폼) + .NET 4.7.2** |
| 예외 처리 | 반환값 검사 | **`IksungProtocolException` / `IksungTimeoutException` / `ChannelDisconnectedException`** |
| 자동 감지 | 폴링 루프 직접 작성 | **`TagDetected` 이벤트 + AutoReconnect** |
| 시스템 진단 | 별도 도구 필요 | **`PingAsync` / `GetReaderInfoAsync` / Raw 패킷 로그** |

---

"""


# ── 챕터 헤더 한 단계 낮춤 ───────────────────────────────────
def shift_headers(text: str) -> str:
    """챕터 .md 의 `# 제목` → `## 제목` 으로 한 단계 들여쓰기.
    코드블록 내부의 `#` 는 건드리지 않음."""
    out = []
    in_code = False
    for line in text.split('\n'):
        if line.startswith('```'):
            in_code = not in_code
            out.append(line)
            continue
        if not in_code and re.match(r'^#{1,5} ', line):
            line = '#' + line
        out.append(line)
    return '\n'.join(out)


# ── 챕터 끝의 [← 이전][다음 →] 네비게이션 제거 ──────────────
NAV_RE = re.compile(r'\n*\[←[^\]]*\]\([^\)]*\)\s*\|\s*\[[^\]]*→?\]\([^\)]*\)\s*$', re.M)
NAV_RE2 = re.compile(r'\n*\[←[^\]]*\]\([^\)]*\)\s*\|\s*\[목차[^\]]*→?\]\([^\)]*\)\s*$', re.M)

def strip_nav(text: str) -> str:
    text = NAV_RE2.sub('', text)
    text = NAV_RE.sub('', text)
    return text


# ── A4 인쇄용 CSS ────────────────────────────────────────────
CSS = """
@page { size: A4; margin: 22mm 18mm 22mm 18mm; }
* { box-sizing: border-box; }
body {
    font-family: 'Noto Sans KR', 'Inter', -apple-system, sans-serif;
    color: #1F2937; font-size: 10.5pt; line-height: 1.65;
    -webkit-print-color-adjust: exact; print-color-adjust: exact;
    margin: 0; padding: 0;
}
h1 { font-size: 26pt; font-weight: 800; color: #0E3A8A;
     border-bottom: 3px solid #0E3A8A; padding-bottom: 8mm; margin: 0 0 10mm;
     letter-spacing: -0.01em; }
h2 { font-size: 17pt; font-weight: 800; color: #0E3A8A;
     border-bottom: 1.5px solid #14B8A6; padding-bottom: 3mm; margin: 12mm 0 5mm;
     page-break-before: always; }
h2:first-of-type { page-break-before: avoid; }
h3 { font-size: 13.5pt; font-weight: 700; color: #134E4A; margin: 8mm 0 3mm; }
h4 { font-size: 11.5pt; font-weight: 700; color: #134E4A; margin: 6mm 0 2mm; }
p  { margin: 0 0 3mm; }
strong { color: #0E3A8A; font-weight: 700; }
em { color: #374151; }
a  { color: #0E3A8A; text-decoration: none; }
a:hover { text-decoration: underline; }

ul, ol { margin: 0 0 4mm; padding-left: 7mm; }
li { margin-bottom: 1mm; }

table { border-collapse: collapse; width: 100%; margin: 0 0 5mm; font-size: 9.5pt; }
th, td { border: 1px solid #D1D5DB; padding: 1.8mm 3mm; text-align: left; vertical-align: top; }
th { background: #F0F6FB; color: #0E3057; font-weight: 700; }
tr:nth-child(even) td { background: #F9FAFB; }

code { font-family: 'JetBrains Mono', Consolas, monospace; font-size: 9pt;
       background: #F3F4F6; padding: 0.5mm 1.5mm; border-radius: 1mm;
       color: #B91C1C; }
pre { background: #1F2937; color: #F9FAFB; padding: 4mm 5mm;
      border-radius: 2mm; overflow-x: auto; font-size: 9pt; line-height: 1.5;
      margin: 0 0 5mm; page-break-inside: avoid; }
pre code { background: transparent; color: inherit; padding: 0; font-size: 9pt; }

blockquote { border-left: 4px solid #F59E0B; padding: 2mm 4mm; margin: 0 0 4mm;
             background: #FFF8E1; color: #1F2937; font-size: 10pt; }

hr { border: none; border-top: 1px solid #E5E7EB; margin: 8mm 0; }

/* TOC */
.toc { background: #F9FAFB; border: 1px solid #E5E7EB; padding: 5mm 8mm;
       margin: 0 0 8mm; border-radius: 2mm; }
.toc ul { padding-left: 5mm; }
.toc li { margin-bottom: 0.8mm; font-size: 10pt; }
"""

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="ko">
<head>
<meta charset="UTF-8">
<title>{title}</title>
<link href="https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@400;500;700;800&family=Inter:wght@500;700;800&family=JetBrains+Mono:wght@400;600&display=swap" rel="stylesheet">
<style>{css}</style>
</head>
<body>
{body}
</body>
</html>
"""


def main():
    body_md = PROLOG.format(build_date=date.today().isoformat())

    for fname in CHAPTERS:
        fpath = os.path.join(HERE, fname)
        if not os.path.exists(fpath):
            print(f'  ! 누락: {fname}')
            continue
        with open(fpath, 'r', encoding='utf-8') as f:
            text = f.read()
        text = strip_nav(text)
        text = shift_headers(text)
        body_md += text.rstrip() + '\n\n---\n\n'
        print(f'  + {fname}  ({len(text):,} chars)')

    # ── 통합 .md 출력 ────────────────────────────────────────
    md_out = os.path.join(HERE, 'Iksung_Reader_SDK_Manual.md')
    with open(md_out, 'w', encoding='utf-8') as f:
        f.write(body_md)
    print(f'\n  ✓ {os.path.basename(md_out)}  ({len(body_md):,} chars · {body_md.count(chr(10))} lines)')

    # ── HTML 변환 ────────────────────────────────────────────
    body_html = markdown.markdown(
        body_md,
        extensions=['fenced_code', 'tables', 'toc', 'sane_lists'],
        extension_configs={'toc': {'title': '목차', 'toc_depth': '2-3'}},
    )
    html = HTML_TEMPLATE.format(
        title='Iksung.Reader SDK — 통합 사용 설명서',
        css=CSS,
        body=body_html,
    )
    html_out = os.path.join(HERE, 'Iksung_Reader_SDK_Manual.html')
    with open(html_out, 'w', encoding='utf-8') as f:
        f.write(html)
    print(f'  ✓ {os.path.basename(html_out)}  ({len(html):,} chars)')

    print('\nNext: node build-sdk-manual-pdf.js')


if __name__ == '__main__':
    main()
