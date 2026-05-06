/*
 * Sample 10 – ISO 7816 USIM / Smart Card
 * =========================================
 * 리더기 내부 USIM 슬롯(또는 외부 스마트카드)을 이용한 ISO 7816 T=0 통신 예제:
 *   1. 카드 활성화 → ATR 수신
 *   2. SELECT MF (Master File) APDU
 *   3. SELECT EF_ICCID (파일 선택)
 *   4. READ BINARY → ICCID 읽기
 *   5. 카드 비활성화
 *
 * ICCID는 SIM 카드의 고유 식별번호 (20자리)입니다.
 *
 * 사용법:
 *   dotnet run -- COM3
 *   dotnet run -- COM3 115200 0    (채널 0)
 */

using Iksung.Reader;
using Iksung.Reader.Exceptions;

string portName = args.Length > 0 ? args[0] : "COM3";
byte   channel  = args.Length > 2 && byte.TryParse(args[2], out byte ch) ? ch : (byte)0;

Console.WriteLine($"[IKSUNG] Connecting to {portName}...");
await using var reader = await IksungReader.ConnectSerialAsync(portName);
Console.WriteLine($"[IKSUNG] Firmware : {await reader.ReadVersionAsync()}");
Console.WriteLine($"[IKSUNG] Channel  : {channel}");
Console.WriteLine("[IKSUNG] Press Ctrl+C to exit.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        // ── 1. 활성화 → ATR ──
        Console.WriteLine("─────────────────────────────────────────────────");
        byte[] atr = await reader.UsimActivateAsync(channel, 3000, cts.Token);
        Console.WriteLine($"ATR : {Hex(atr)}");
        Console.WriteLine($"      Protocol: {ParseProtocol(atr)}");

        // ── 2. SELECT MF (3F 00) ──
        // T=0 SELECT: CLA=00 INS=A4 P1=00 P2=00 Lc=02 Data=3F00
        byte[] selectMf  = [0x00, 0xA4, 0x00, 0x00, 0x02, 0x3F, 0x00];
        byte[] resp      = await reader.UsimSendTpduAsync(selectMf, channel, 2000, cts.Token);
        Console.WriteLine($"SELECT MF     : {Hex(resp)}  SW={Sw(resp)}");

        // ── 3. SELECT EF_ICCID (2F E2) ──
        byte[] selectEf  = [0x00, 0xA4, 0x00, 0x00, 0x02, 0x2F, 0xE2];
        resp             = await reader.UsimSendTpduAsync(selectEf, channel, 2000, cts.Token);
        Console.WriteLine($"SELECT EF_ICCID: {Hex(resp)}  SW={Sw(resp)}");

        bool canRead = resp.Length >= 2 && resp[^2] == 0x90 && resp[^1] == 0x00;
        if (!canRead && resp.Length >= 2 && resp[^2] == 0x9F)
        {
            // GET RESPONSE (T=0)
            byte le       = resp[^1];
            byte[] getResp = [0x00, 0xC0, 0x00, 0x00, le];
            resp           = await reader.UsimSendTpduAsync(getResp, channel, 2000, cts.Token);
            canRead        = resp.Length >= 2 && resp[^2] == 0x90;
        }

        if (canRead)
        {
            // ── 4. READ BINARY → ICCID 10바이트 ──
            byte[] readBin = [0x00, 0xB0, 0x00, 0x00, 0x0A];
            resp           = await reader.UsimSendTpduAsync(readBin, channel, 2000, cts.Token);
            Console.WriteLine($"READ BINARY   : {Hex(resp)}  SW={Sw(resp)}");
            if (resp.Length >= 12 && resp[^2] == 0x90)
            {
                byte[] iccidBcd = resp[..10];
                Console.WriteLine($"ICCID (BCD)   : {BcdDecode(iccidBcd)}");
            }
        }

        // ── 5. 비활성화 ──
        await reader.UsimDeactivateAsync(channel, 1000, cts.Token);
        Console.WriteLine("Card deactivated.\n");
    }
    catch (IksungProtocolException ex) { Console.WriteLine($"Protocol error: {ex.Message}"); }
    catch (IksungTimeoutException)     { Console.WriteLine("(no card / timeout)"); }
    catch (OperationCanceledException) { break; }

    // 5초마다 반복 (USIM은 연속 접근보다 요청 시 접근)
    try { await Task.Delay(5000, cts.Token); } catch (OperationCanceledException) { break; }
}

Console.WriteLine("\n[IKSUNG] Done.");

static string Hex(byte[] b)  => BitConverter.ToString(b).Replace("-", " ");
static string Sw(byte[] b)   => b.Length >= 2 ? $"{b[^2]:X2}{b[^1]:X2}" : "??";

// ATR 프로토콜 파싱 (간략)
static string ParseProtocol(byte[] atr)
{
    if (atr.Length < 2) return "unknown";
    // T0 byte at atr[1] — TA1,TB1,TC1,TD1 presence flags
    bool hasT1 = atr.Length >= 2 && (atr[1] & 0x0F) == 1;
    return hasT1 ? "T=1" : "T=0";
}

// BCD 디코드: 각 니블을 숫자로 변환 (ICCID는 nibble-swap BCD)
static string BcdDecode(byte[] bcd)
{
    var sb = new System.Text.StringBuilder();
    foreach (byte b in bcd)
    {
        sb.Append((char)('0' + (b & 0x0F)));
        sb.Append((char)('0' + (b >> 4)));
    }
    return sb.ToString();
}
