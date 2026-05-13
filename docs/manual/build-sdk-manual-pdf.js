// build-sdk-manual-pdf.js — Iksung_Reader_SDK_Manual.html → A4 PDF (헤더/푸터/페이지번호)
//
// 사용법:
//   node build-sdk-manual-pdf.js
//
// 입력: Iksung_Reader_SDK_Manual.html  (먼저 python build-sdk-manual.py 로 생성)
// 출력: Iksung_Reader_SDK_Manual.pdf
//
// 의존성: puppeteer-core + 시스템 Chrome
// (ISManual 의 node_modules 를 그대로 활용하여 SDK 폴더에 별도 npm 설치 불필요)

const fs   = require('fs');
const path = require('path');

// ── puppeteer-core 위치: ISManual 의 node_modules 사용 ─────
const SHARED_MODULES = 'D:\\Work\\ISManual\\node_modules';
let puppeteer;
try {
  puppeteer = require(path.join(SHARED_MODULES, 'puppeteer-core'));
} catch (e) {
  console.error('✗ puppeteer-core not found at: ' + SHARED_MODULES);
  console.error('  → cd D:/Work/ISManual && npm install puppeteer-core 를 실행하거나');
  console.error('  → 본 스크립트의 SHARED_MODULES 경로를 수정하세요.');
  process.exit(1);
}

// ── Chrome 찾기 ────────────────────────────────────────────
const chromeCandidates = [
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
];
const chromePath = chromeCandidates.find(p => fs.existsSync(p));
if (!chromePath) {
  console.error('✗ Chrome / Edge not found.');
  process.exit(1);
}

// ── 입출력 경로 ────────────────────────────────────────────
const inputPath  = path.resolve(__dirname, 'Iksung_Reader_SDK_Manual.html');
const outputPath = path.resolve(__dirname, 'Iksung_Reader_SDK_Manual.pdf');

if (!fs.existsSync(inputPath)) {
  console.error('✗ 입력 HTML 없음: ' + inputPath);
  console.error('  → 먼저 python build-sdk-manual.py 를 실행하세요.');
  process.exit(1);
}

// ── 헤더 / 푸터 (puppeteer 가 ~75% 로 스케일하므로 폰트는 작게 설계) ──
const HEADER_TEMPLATE = `
<div style="font-family:'Inter','Noto Sans KR',sans-serif;font-size:9px;color:#6B7280;width:100%;padding:4mm 14mm 0;display:flex;justify-content:space-between;align-items:center;border-bottom:0.4px solid #D1D5DB;padding-bottom:2.5mm;-webkit-print-color-adjust:exact;">
  <span style="font-family:Inter,sans-serif;font-weight:800;color:#0E3A8A;letter-spacing:0.18em;">IKSUNG&nbsp;ELECTRONICS</span>
  <span style="color:#374151;font-weight:600;">Iksung.Reader .NET SDK &nbsp;·&nbsp; 통합 사용 설명서</span>
  <span style="font-family:Inter,sans-serif;font-weight:600;color:#6B7280;">V1.0</span>
</div>`;

const FOOTER_TEMPLATE = `
<div style="font-family:'Inter','Noto Sans KR',sans-serif;font-size:9px;color:#6B7280;width:100%;padding:0 14mm 4mm;display:flex;justify-content:space-between;align-items:center;border-top:0.4px solid #D1D5DB;padding-top:2.5mm;-webkit-print-color-adjust:exact;">
  <span style="color:#6B7280;">익성전자 &nbsp;·&nbsp; Iksung.Reader SDK &nbsp;·&nbsp; NuGet · .NET 8 + .NET 4.7.2 · async/await</span>
  <span style="font-family:Inter,sans-serif;font-weight:700;color:#0E3A8A;">Page&nbsp;<span class="pageNumber"></span>&nbsp;/&nbsp;<span class="totalPages"></span></span>
</div>`;

(async () => {
  console.log(`▶ Browser : ${chromePath}`);
  console.log(`▶ Input   : ${inputPath}`);
  console.log(`▶ Output  : ${outputPath}`);

  const browser = await puppeteer.launch({
    executablePath: chromePath,
    headless: 'new',
    args: ['--no-sandbox', '--disable-setuid-sandbox'],
  });

  try {
    const page = await browser.newPage();
    const fileUrl = 'file:///' + inputPath.replace(/\\/g, '/');
    await page.goto(fileUrl, { waitUntil: 'networkidle0', timeout: 60000 });

    await page.evaluateHandle('document.fonts.ready');
    // 이미지가 있다면 디코딩 완료 대기
    await page.evaluate(async () => {
      const imgs = Array.from(document.images);
      await Promise.all(imgs.map(img => img.decode().catch(() => null)));
    });

    await page.pdf({
      path: outputPath,
      format: 'A4',
      printBackground: true,
      preferCSSPageSize: false,
      displayHeaderFooter: true,
      headerTemplate: HEADER_TEMPLATE,
      footerTemplate: FOOTER_TEMPLATE,
      margin: { top: '22mm', right: '14mm', bottom: '18mm', left: '14mm' },
    });

    const stat = fs.statSync(outputPath);
    console.log(`✓ PDF generated: ${outputPath}  (${(stat.size / 1024).toFixed(1)} KB)`);
  } finally {
    await browser.close();
  }
})().catch(err => {
  console.error('✗ Build failed:', err);
  process.exit(1);
});
