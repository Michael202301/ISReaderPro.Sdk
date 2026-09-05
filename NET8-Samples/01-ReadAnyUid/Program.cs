/*
 * Sample 01 – Read Any UID
 * ========================
 * 리더기에 연결해 NFC/RFID 카드 UID를 계속 읽어서 출력합니다.
 * ISO 14443-A/B, ISO 15693, LF 125 kHz 순서로 시도합니다.
 *
 * 사용법:
 *   (인수 없이 실행)                리더를 자동으로 찾습니다 — 포트를 몰라도 됩니다
 *   dotnet run -- COM3                      (Serial — Windows)
 *   dotnet run -- /dev/ttyUSB0              (Serial — Linux)
 *   dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

// ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
string firstArg = args.Length > 0 ? args[0] : "(자동 탐색)";

// ── 연결: 인수 없으면 자동 탐색 / "COMx" = Serial / "pcsc[:이름]" = PC/SC ──
IksungReader? reader = await SampleConnect.OpenAsync(args);
if (reader == null) return;
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
Console.WriteLine($"[IKSUNG] Firmware: {await reader.ReadVersionAsync()}");
Console.WriteLine("[IKSUNG] Place a card on the reader. Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    string? uidHex = null;
    string? cardKind = null;

    // ── ISO 14443-A ──
    try
    {
        byte[] uid = await reader.ReadIso14443aUidAsync(500, cts.Token);
        uidHex   = BitConverter.ToString(uid).Replace("-", "");
        cardKind = "ISO 14443-A";
    }
    catch (IksungProtocolException) { }   // 카드 없음
    catch (IksungTimeoutException)  { }

    // ── ISO 14443-B ──
    if (uidHex == null)
    {
        try
        {
            byte[] uid = await reader.ReadIso14443bUidAsync(500, cts.Token);
            uidHex   = BitConverter.ToString(uid).Replace("-", "");
            cardKind = "ISO 14443-B";
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── ISO 15693 ──
    if (uidHex == null)
    {
        try
        {
            byte[] uid = await reader.ReadIso15693UidAsync(500, cts.Token);
            uidHex   = BitConverter.ToString(uid).Replace("-", "");
            cardKind = "ISO 15693";
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    // ── LF 125 kHz ──
    if (uidHex == null)
    {
        try
        {
            byte[] uid = await reader.ReadLf125KhzUidAsync(800, cts.Token);
            uidHex   = BitConverter.ToString(uid).Replace("-", "");
            cardKind = "LF 125 kHz";
        }
        catch (IksungProtocolException) { }
        catch (IksungTimeoutException)  { }
    }

    if (uidHex != null)
        Console.WriteLine($"{DateTime.Now:HH:mm:ss}  [{cardKind,-12}]  UID: {uidHex}");

    try { await Task.Delay(200, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");
