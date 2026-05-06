/*
 * Sample 01 – Read Any UID
 * ========================
 * 리더기에 연결해 NFC/RFID 카드 UID를 계속 읽어서 출력합니다.
 * ISO 14443-A/B, ISO 15693, LF 125 kHz 순서로 시도합니다.
 *
 * 사용법:
 *   dotnet run -- COM3          (Windows)
 *   dotnet run -- /dev/ttyUSB0  (Linux)
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

string portName = args.Length > 0 ? args[0] : "COM3";
int    baudRate = args.Length > 1 && int.TryParse(args[1], out int b) ? b : 115200;

Console.WriteLine($"[IKSUNG] Connecting to {portName} @ {baudRate} baud...");

await using var reader = await IksungReader.ConnectSerialAsync(portName, baudRate);
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
