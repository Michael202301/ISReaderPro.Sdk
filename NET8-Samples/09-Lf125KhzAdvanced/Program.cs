/*
 * Sample 09 – LF 125 kHz Advanced
 * ==================================
 * 다양한 LF 125 kHz 카드 포맷을 읽는 예제입니다:
 *   - EM410X  : 공장/출입 관리 시스템에서 가장 흔한 포맷 (5바이트 UID)
 *   - ISO 11784/11785 FDX-B : 동물 ID 칩 (15자리 국가코드 + 개체번호)
 *   - SECOM   : 국내 보안 시스템 전용 블록 포맷
 *   - Temic   : T5577 범용 재기록 가능 LF 칩
 *   - Raw bits: 원시 RF 비트 스트림
 *   - HTRC 자동 튜닝
 *
 * 사용법:
 *   dotnet run -- COM3                      (Serial — Windows)
 *   dotnet run -- /dev/ttyUSB0              (Serial — Linux)
 *   dotnet run -- COM3 115200 em410x        (Serial, 특정 포맷만 반복)
 *   dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 *
 * 플랫폼:
 * ┌──────────────────────────────────────────────────────────────────────────┐
 * │  프로젝트가 x86 으로 고정되어 있습니다                                  │
 * │                                                                          │
 * │  .NET 8 의 SerialPort 는 x64 / x86 모두 안정적입니다. 이 솔루션은      │
 * │  NET4x-Samples 와 동일한 x86 플랫폼 설정을 사용합니다.                 │
 * │  (x64 빌드에서도 정상 동작합니다)                                       │
 * │                                                                          │
 * │  Visual Studio: 구성 관리자 → 플랫폼 → x86                             │
 * │  CLI: dotnet build -p:Platform=x86 / dotnet run -r win-x86             │
 * └──────────────────────────────────────────────────────────────────────────┘
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

// ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
string firstArg = args.Length > 0 ? args[0] : "COM3";
string mode     = args.Length > 2 ? args[2].ToLower() : "all";

IksungReader reader;
if (firstArg.StartsWith("pcsc", StringComparison.OrdinalIgnoreCase))
{
    string? readerName = firstArg.Contains(':')
        ? firstArg.Substring(firstArg.IndexOf(':') + 1).Trim()
        : null;
    if (readerName == null)
    {
        var readers = IksungPcscDiscovery.GetAvailableReaders();
        if (readers.Count == 0)
        {
            Console.WriteLine("[ERROR] PC/SC 리더를 찾을 수 없습니다.");
            Console.WriteLine("  - USB CCID 리더(IS-3500Z 등)가 연결되어 있는지 확인하세요.");
            Console.WriteLine("  - Windows Smart Card Service(SCardSvr)가 실행 중인지 확인하세요.");
            return;
        }
        readerName = readers[0];
        Console.WriteLine($"[IKSUNG] PC/SC 리더 자동 선택: {readerName}");
    }
    else
    {
        Console.WriteLine($"[IKSUNG] PC/SC 연결: {readerName}");
    }
    reader = await IksungReader.ConnectPcscAsync(readerName);
}
else
{
    Console.WriteLine($"[IKSUNG] Serial 연결: {firstArg}");
    reader = await IksungReader.ConnectSerialAsync(firstArg);
}
await using var _ = reader;
// ── 연결 확인 ─────────────────────────────────────────────────
Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
if (!await reader.PingAsync(timeoutMs: 500))
{
    Console.WriteLine($"[ERROR] 리더기가 응답하지 않습니다. (연결: {firstArg})");
    Console.WriteLine("  - 리더기 전원을 확인하세요.");
    Console.WriteLine("  - 케이블 연결을 확인하세요.");
    return;
}
Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}");
Console.WriteLine("[IKSUNG] Place an LF 125 kHz card. Press Ctrl+C to exit.\n");

// ── HTRC 샘플링 자동 조정 (최초 1회) ──
Console.Write("[IKSUNG] Auto-tuning HTRC sampling... ");
try
{
    byte[] tuned = await reader.AutoTuneLfSamplingAsync(5000);
    Console.WriteLine($"OK (value=0x{tuned[0]:X2})");
}
catch (IksungProtocolException ex) { Console.WriteLine($"skipped ({ex.Message})"); }

Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    bool found = false;

    // ── EM410X ──
    if (mode is "all" or "em410x")
    {
        try
        {
            byte[] uid = await reader.ReadLf125KhzUidAsync(800, cts.Token);
            PrintTag("EM410X", uid, FormatEm410X(uid));
            found = true;
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── ISO 11784/11785 (FDX-B 동물 칩) ──
    if (!found && mode is "all" or "iso11784")
    {
        try
        {
            byte[] raw = await reader.ReadLfIso11784Async(1000, cts.Token);
            string decoded = ParseIso11784(raw);
            PrintTag("ISO11784 FDX-B", raw, decoded);
            found = true;
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── SECOM ──
    if (!found && mode is "all" or "secom")
    {
        try
        {
            byte[] secom = await reader.ReadLfSecomBlockAsync(1000, cts.Token);
            PrintTag("SECOM block", secom, "");
            found = true;
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── Temic ──
    if (!found && mode is "all" or "temic")
    {
        try
        {
            byte[] temic = await reader.ReadLfTemicBlockAsync(0x40, 0x0A, 1000, cts.Token);
            PrintTag("Temic block", temic, "");
            found = true;
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── Raw bits ──
    if (!found && mode is "raw")
    {
        try
        {
            byte[] bits = await reader.ReadLfRawBitsAsync(64, 1500, cts.Token);
            PrintTag("Raw 64 bits", bits, ToBitString(bits, 64));
            found = true;
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    try { await Task.Delay(found ? 300 : 50, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static void PrintTag(string label, byte[] raw, string decoded)
{
    string ts = DateTime.Now.ToString("HH:mm:ss");
    Console.Write($"{ts}  [{label,-18}]  {BitConverter.ToString(raw).Replace("-", " ")}");
    if (!string.IsNullOrEmpty(decoded)) Console.Write($"  → {decoded}");
    Console.WriteLine();
}

// EM410X: 5바이트 → 10자리 16진수 (일반 표기 방식)
static string FormatEm410X(byte[] uid)
    => uid.Length == 5 ? BitConverter.ToString(uid).Replace("-", "") : "";

// ISO11784 FDX-B: 원시 바이트에서 국가코드 + 개체번호 파싱
static string ParseIso11784(byte[] data)
{
    if (data.Length < 8) return $"({data.Length}B — insufficient)";
    // FDX-B 64-bit: bit 0-37=국가코드(10)+개체번호(38), 전형적인 구현 따라 다름
    ulong bits = 0;
    for (int i = 0; i < 8; i++) bits |= ((ulong)data[i]) << (i * 8);
    ulong animalId  = bits & 0x3FFFFFFFFF;      // 38 bits
    ulong countryId = (bits >> 38) & 0x3FF;      // 10 bits
    return $"Country={countryId:D3} Animal={animalId:D12}";
}

static string ToBitString(byte[] data, int bitCount)
{
    var sb = new System.Text.StringBuilder();
    for (int i = 0; i < bitCount && (i / 8) < data.Length; i++)
    {
        sb.Append((data[i / 8] >> (7 - (i % 8))) & 1);
        if ((i + 1) % 8 == 0) sb.Append(' ');
    }
    return sb.ToString().Trim();
}
