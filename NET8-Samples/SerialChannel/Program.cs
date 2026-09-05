// =============================================================================
// Serial (COM / USB-VCP) Channel
// =============================================================================
// 시리얼(COM 포트 · USB 가상 COM) 채널로 IS-NFC 시리즈 리더기에 연결하여
// 다음을 시연합니다:
//   A. 포트 결정 (인수로 지정)
//   B. 연결
//   C. 응답 확인 + 버전 정보
//   D. 카드 UID 읽기 (ReadAllUidAsync)
//   E. 버저
//   F. RF Off / On
//   G. AutoRead 데모 (TagDetected 이벤트)
//
// ┌─ 참고 ────────────────────────────────────────────────────────────────────┐
// │  시리얼은 크로스플랫폼입니다 (Windows · Linux · macOS).                     │
// │  포트는 한 번에 한 프로그램만 열 수 있습니다 (독점).                         │
// │  → 다른 프로그램(ISReaderPro 등)이 포트를 쓰고 있으면 먼저 닫으세요.         │
// │  PC/SC 로 연결하려면 15-PcscChannel 예제를 참고하세요.                       │
// └───────────────────────────────────────────────────────────────────────────┘
//
// 사용법:
//   (인수 없이 실행)                리더를 자동으로 찾습니다 — 포트를 몰라도 됩니다
//   dotnet run -- COM4                     (Windows)
//   dotnet run -- COM4 115200              (보드레이트 지정)
//   dotnet run -- /dev/ttyUSB0             (Linux)
//   dotnet run -- /dev/tty.usbserial-XXXX  (macOS)
// =============================================================================

using Iksung.Reader;
using Iksung.Reader.Exceptions;

// ─── Section A: 포트 결정 ──────────────────────────────────────────────────

Console.WriteLine("=== Serial (COM/USB) Channel ===");
Console.WriteLine();

int baud = args.Length > 1 && int.TryParse(args[1], out int b) ? b : 115200;

string? port = args.Length > 0 ? args[0] : null;
if (port == null)
{
    // 포트를 안 주면 리더가 붙은 포트를 직접 찾는다.
    Console.WriteLine("[A] 포트를 지정하지 않아 리더를 찾는 중입니다...");
    var found = await IksungReaderDiscovery.ScanIksungPortsAsync(baud);
    if (found.Count == 0)
    {
        var all = IksungReaderDiscovery.GetAllSerialPorts().ToArray();
        Console.WriteLine("    리더가 붙은 시리얼 포트를 찾지 못했습니다.");
        Console.WriteLine(all.Length == 0
            ? "    이 PC에 시리얼 포트가 없습니다."
            : $"    이 PC의 시리얼 포트: {string.Join(", ", all)}");
        Console.WriteLine("    리더가 CCID 모드면 시리얼로는 잡히지 않습니다 → 15-PcscChannel 예제를 사용하세요.");
        return;
    }
    port = found[0];
    Console.WriteLine($"    발견: {port}");
}
Console.WriteLine($"[A] 포트: {port} @ {baud} bps");
Console.WriteLine();

// ─── Section B: 연결 ───────────────────────────────────────────────────────

Console.WriteLine("[B] 시리얼 리더에 연결 중...");
IksungReader reader;
try
{
    reader = await IksungReader.ConnectSerialAsync(port, baud);
}
catch (IksungException ex)
{
    Console.WriteLine($"    연결 실패: {ex.Message}");
    Console.WriteLine("    - 포트 이름이 맞는지 확인하세요 (Windows: 장치 관리자 → 포트(COM & LPT)).");
    Console.WriteLine("    - 다른 프로그램(ISReaderPro 등)이 이 포트를 사용 중이면 먼저 닫으세요.");
    Console.WriteLine("      (시리얼 포트는 한 번에 한 프로그램만 열 수 있습니다.)");
    return;
}

await using (reader)
{
    Console.WriteLine($"    연결 성공! 채널: {reader.ConnectedVia}");
    Console.WriteLine();

    // ─── Section C: 응답 확인 + 버전 ───────────────────────────────────

    Console.WriteLine("[C] 리더 응답 확인 + 버전 읽기...");
    if (!await reader.PingAsync(timeoutMs: 500))
    {
        Console.WriteLine("    리더가 응답하지 않습니다 (전원 · 케이블 확인).");
        return;
    }
    Console.WriteLine($"    FW 버전: {await reader.ReadVersionAsync()}");
    Console.WriteLine();

    // ─── Section D: 카드 UID 읽기 ──────────────────────────────────────

    Console.WriteLine("[D] 카드 UID 읽기...");
    Console.WriteLine("    카드/태그를 리더기에 올려 놓은 후 Enter 를 누르세요.");
    Console.ReadLine();
    try
    {
        byte[] uid = await reader.ReadAllUidAsync();
        if (uid.Length == 0)
            Console.WriteLine("    감지된 카드 없음 (빈 응답).");
        else
            Console.WriteLine($"    UID: {BitConverter.ToString(uid).Replace("-", " ")}  ({uid.Length} bytes)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    UID 읽기 실패: {ex.Message}");
    }
    Console.WriteLine();

    // ─── Section E: 버저 ───────────────────────────────────────────────

    Console.WriteLine("[E] 버저 동작 테스트...");
    try
    {
        await reader.BuzzerAsync();
        Console.WriteLine("    버저 명령 전송 완료.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    버저 실패: {ex.Message}");
    }
    Console.WriteLine();

    // ─── Section F: RF Off / On ────────────────────────────────────────

    Console.WriteLine("[F] RF Off → 500ms 대기 → RF On...");
    try
    {
        await reader.RfOffAsync();
        await Task.Delay(500);
        await reader.RfOnAsync();
        Console.WriteLine("    RF 제어 완료.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    RF 제어 실패: {ex.Message}");
    }
    Console.WriteLine();

    // ─── Section G: AutoRead 데모 ──────────────────────────────────────

    Console.WriteLine("[G] AutoRead 데모 — 카드를 올리면 UID 가 출력됩니다.");
    Console.WriteLine("    종료하려면 아무 키나 누르세요.");
    Console.WriteLine();

    int tagCount = 0;
    reader.TagDetected += (_, e) =>
    {
        tagCount++;
        Console.WriteLine($"    [{tagCount:D3}] 태그 감지! 타입={e.CardType}  UID={e.UidHex}");
    };

    try
    {
        await reader.StartAutoReadAsync();
        if (Console.IsInputRedirected) Console.ReadLine(); else Console.ReadKey(intercept: true);
        await reader.StopAutoReadAsync();
        Console.WriteLine();
        Console.WriteLine($"    AutoRead 종료. 총 감지 횟수: {tagCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    AutoRead 실패: {ex.Message}");
    }
    Console.WriteLine();

    Console.WriteLine("=== 샘플 종료 ===");
}
