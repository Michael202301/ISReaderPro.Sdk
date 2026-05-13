// =============================================================================
// Sample 15 – PC/SC (CCID) Channel
// =============================================================================
// 이 샘플은 PC/SC (CCID) 채널로 IS-NFC 시리즈 리더기에 연결하여
// 다음 기능을 시연합니다:
//   A. 연결된 PC/SC 리더 목록 조회
//   B. 리더 자동 선택 (인수로 지정 가능)
//   C. 리더 연결
//   D. 리더 버전 정보 읽기
//   E. 카드 UID 읽기 (ReadAllUidAsync)
//   F. 버저 동작 테스트
//   G. RF Off / On
//   H. AutoRead 데모 (TagDetected 이벤트)
//
// ┌─ 주의 ────────────────────────────────────────────────────────────────────┐
// │  PC/SC 는 Windows 전용입니다. Smart Card Service(SCardSvr)가              │
// │  실행 중이어야 합니다.                                                     │
// │  빌드 플랫폼 설정이 x86 으로 되어 있습니다 (winscard.dll 호환성).          │
// └───────────────────────────────────────────────────────────────────────────┘
//
// 사용법:
//   dotnet run                         → 첫 번째 감지 리더에 자동 연결
//   dotnet run "iksung IS-3500Z 0"     → 지정 리더에 연결
// =============================================================================

using Iksung.Reader;
using Iksung.Reader.Exceptions;

// ─── Section A: 리더 목록 조회 ────────────────────────────────────────────

Console.WriteLine("=== Sample 15: PC/SC (CCID) Channel ===");
Console.WriteLine();
Console.WriteLine("[A] 연결된 PC/SC 리더 목록 조회 중...");

var readers = IksungPcscDiscovery.GetAvailableReaders();

if (readers.Count == 0)
{
    Console.WriteLine("    리더를 찾을 수 없습니다.");
    Console.WriteLine("    - USB CCID 리더가 연결되어 있는지 확인하세요.");
    Console.WriteLine("    - Windows Smart Card Service(SCardSvr)가 실행 중인지 확인하세요.");
    Console.WriteLine("      (서비스 → '스마트 카드' 검색 → 시작)");
    return;
}

Console.WriteLine($"    발견된 리더 ({readers.Count}개):");
for (int i = 0; i < readers.Count; i++)
    Console.WriteLine($"      [{i}] {readers[i]}");
Console.WriteLine();

// ─── Section B: 리더 선택 ─────────────────────────────────────────────────

string readerName;
if (args.Length > 0)
{
    readerName = args[0];
    Console.WriteLine($"[B] 지정된 리더 사용: {readerName}");
}
else
{
    readerName = readers[0];
    Console.WriteLine($"[B] 첫 번째 리더 자동 선택: {readerName}");
}
Console.WriteLine();

// ─── Section C: 연결 ───────────────────────────────────────────────────────

Console.WriteLine("[C] PC/SC 리더에 연결 중...");
IksungReader reader;
try
{
    reader = await IksungReader.ConnectPcscAsync(readerName);
}
catch (ChannelDisconnectedException ex)
{
    Console.WriteLine($"    연결 실패: {ex.Message}");
    Console.WriteLine("    - 리더기에 카드/태그가 삽입되어 있어야 PC/SC 연결이 가능합니다.");
    Console.WriteLine("    - 일부 리더는 카드 없이도 연결되나 리더 모델에 따라 다릅니다.");
    return;
}

await using (reader)
{
    Console.WriteLine($"    연결 성공! 채널: {reader.ConnectedVia}");
    Console.WriteLine();

    // ─── Section D: 리더 버전 정보 ───────────────────────────────────────

    Console.WriteLine("[D] 리더 버전 정보 읽기...");
    try
    {
        string version = await reader.ReadVersionAsync();
        Console.WriteLine($"    FW 버전: {version}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    버전 읽기 실패: {ex.Message}");
    }
    Console.WriteLine();

    // ─── Section E: 카드 UID 읽기 ────────────────────────────────────────

    Console.WriteLine("[E] 카드 UID 읽기...");
    Console.WriteLine("    카드/태그를 리더기에 올려 놓은 후 Enter 를 누르세요.");
    Console.ReadLine();
    try
    {
        byte[] uid = await reader.ReadAllUidAsync();
        if (uid.Length == 0)
        {
            Console.WriteLine("    감지된 카드 없음 (빈 응답).");
        }
        else
        {
            string hex = BitConverter.ToString(uid).Replace("-", " ");
            Console.WriteLine($"    UID: {hex}  ({uid.Length} bytes)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    UID 읽기 실패: {ex.Message}");
    }
    Console.WriteLine();

    // ─── Section F: 버저 테스트 ──────────────────────────────────────────

    Console.WriteLine("[F] 버저 동작 테스트...");
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

    // ─── Section G: RF Off / On ───────────────────────────────────────────

    Console.WriteLine("[G] RF Off → 500ms 대기 → RF On...");
    try
    {
        await reader.RfOffAsync();
        Console.WriteLine("    RF Off 완료.");
        await Task.Delay(500);
        await reader.RfOnAsync();
        Console.WriteLine("    RF On 완료.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    RF 제어 실패: {ex.Message}");
    }
    Console.WriteLine();

    // ─── Section H: AutoRead 데모 ─────────────────────────────────────────

    Console.WriteLine("[H] AutoRead 데모 시작...");
    Console.WriteLine("    카드를 리더기에 올리면 UID 가 출력됩니다.");
    Console.WriteLine("    종료하려면 아무 키나 누르세요.");
    Console.WriteLine();

    int tagCount = 0;
    reader.TagDetected += (_, e) =>
    {
        tagCount++;
        // TagDetectedEventArgs: CardType, Uid (byte[]), UidHex, Cmd1, Cmd2, RawData
        Console.WriteLine($"    [{tagCount:D3}] 태그 감지! 타입={e.CardType}  UID={e.UidHex}");
    };

    try
    {
        await reader.StartAutoReadAsync();
        Console.ReadKey(intercept: true);
        await reader.StopAutoReadAsync();
        Console.WriteLine();
        Console.WriteLine($"    AutoRead 종료. 총 감지 횟수: {tagCount}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    AutoRead 실패: {ex.Message}");
        Console.WriteLine("    PC/SC 채널에서 AutoRead 는 폴링 방식으로 동작합니다.");
        Console.WriteLine("    리더기가 AutoRead 명령을 지원하는지 확인하세요.");
    }
    Console.WriteLine();

    Console.WriteLine("=== 샘플 종료 ===");
}
