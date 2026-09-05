/*
 * Hello World — 가장 먼저 실행해 볼 예제
 * =====================================
 * 리더에 연결해 펌웨어 버전과 고유 ID를 출력합니다.
 *
 * 사용법:
 *   dotnet run                      리더 자동 탐색 (권장 — 포트를 몰라도 됩니다)
 *   dotnet run -- COM40             Serial 포트 지정
 *   dotnet run -- COM40 115200      보드레이트까지 지정
 *   dotnet run -- pcsc              PC/SC(USB CCID) 리더
 *   dotnet run -- pcsc:iksung       PC/SC 리더 이름 일부로 지정
 */

using Iksung.Reader;

// ── 연결: 인수가 없으면 리더를 자동으로 찾습니다 ──────────────────────────
IksungReader? reader = await SampleConnect.OpenAsync(args);
if (reader == null) return;
await using var _ = reader;

// ── 연결 확인 ─────────────────────────────────────────────────
Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
if (!await reader.PingAsync(timeoutMs: 500))
{
    Console.WriteLine("[ERROR] 리더기가 응답하지 않습니다.");
    Console.WriteLine("  - 리더기 전원과 케이블 연결을 확인하세요.");
    Console.WriteLine("  - 다른 프로그램(ISReaderPro 등)이 리더를 잡고 있지 않은지 확인하세요.");
    return;
}

Console.WriteLine($"Connected via : {reader.ConnectedVia}");
Console.WriteLine($"Firmware ver  : {await reader.ReadVersionAsync()}");
Console.WriteLine($"Unique ID     : {BitConverter.ToString(await reader.ReadUniqueIdAsync())}");
