/*
 * Sample 14 – System Features
 * ============================
 * 시스템 레벨 기능 예제:
 *   A. 포트 탐색     — IksungReaderDiscovery.ScanIksungPortsAsync
 *   B. 연결 옵션     — IksungReaderOptions (AutoReconnect, LogRawPackets)
 *   C. 연결 상태 이벤트 — reader.ConnectionChanged
 *   D. 리더기 정보   — reader.GetReaderInfoAsync, reader.PingAsync
 *   E. Raw 패킷 로그 — reader.RawPacketReceived
 *   F. 명시적 해제   — reader.DisconnectAsync
 *
 * 사용법:
 *   dotnet run -- COM3                      (Serial — Windows)
 *   dotnet run -- /dev/ttyUSB0              (Serial — Linux)
 *   dotnet run -- scan                      (Serial 포트 탐색 모드)
 *   dotnet run -- pcsc                      (PC/SC — 첫 번째 리더 자동 선택)
 *   dotnet run -- "pcsc:iksung IS-3500Z 0"  (PC/SC — 리더 이름 직접 지정)
 *   dotnet run -- pcsc-list                 (PC/SC 리더 목록 출력)
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

// ══════════════════════════════════════════════════════════════
// A. 포트 탐색 모드 (dotnet run -- scan)
// ══════════════════════════════════════════════════════════════
if (args.Length > 0 && args[0].Equals("scan", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("[SCAN] Iksung 리더기 포트 탐색 시작...");

    var progress = new Progress<(string portName, bool isIksung)>(p =>
    {
        string mark = p.isIksung ? "✓ Iksung" : "✗";
        Console.WriteLine($"  {p.portName,-8} {mark}");
    });

    var found = await IksungReaderDiscovery.ScanIksungPortsAsync(
        baudRate:      115200,
        pingTimeoutMs: 400,
        progressCallback: progress);

    Console.WriteLine();
    if (found.Count == 0)
        Console.WriteLine("[SCAN] Iksung 리더기를 찾을 수 없습니다.");
    else
        Console.WriteLine($"[SCAN] 발견된 포트: {string.Join(", ", found)}");
    return;
}

// ══════════════════════════════════════════════════════════════
// PC/SC 리더 목록 모드 (dotnet run -- pcsc-list)
// ══════════════════════════════════════════════════════════════
if (args.Length > 0 && args[0].Equals("pcsc-list", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("[PCSC] 연결된 PC/SC 리더 목록:");
    var pcscReaders = IksungPcscDiscovery.GetAvailableReaders();
    if (pcscReaders.Count == 0)
        Console.WriteLine("  (없음)");
    else
        for (int i = 0; i < pcscReaders.Count; i++)
            Console.WriteLine($"  [{i}] {pcscReaders[i]}");
    return;
}

// ══════════════════════════════════════════════════════════════
// B-F. 연결 및 시스템 기능 데모
// ══════════════════════════════════════════════════════════════
// ── 채널 선택: "pcsc" 또는 "pcsc:리더이름" → PC/SC, 그 외 → Serial ──────────
string firstArg = args.Length > 0 ? args[0] : "COM3";

Console.WriteLine($"[IKSUNG] Connecting to {firstArg}...");

// ── B. 연결 옵션 설정 ──────────────────────────────────────────
var options = new IksungReaderOptions
{
    AutoReconnect    = true,        // 케이블 뽑혀도 자동 재연결
    ReconnectDelayMs = 2000,        // 2초마다 재시도
    DefaultTimeoutMs = 1000,
    LogRawPackets    = true,        // TX/RX raw 바이트 이벤트 발생
};

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
    reader = await IksungReader.ConnectPcscAsync(readerName, options);
}
else
{
    Console.WriteLine($"[IKSUNG] Serial 연결: {firstArg}");
    reader = await IksungReader.ConnectSerialAsync(firstArg, 115200, options);
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

// ── C. 연결 상태 이벤트 ───────────────────────────────────────
reader.ConnectionChanged += (_, isConnected) =>
{
    string status = isConnected ? "✓ 연결됨" : "✗ 연결 끊김";
    Console.WriteLine($"\n[CONNECTION] {status}");
};

// ── E. Raw 패킷 로그 이벤트 ───────────────────────────────────
reader.RawPacketReceived += (_, e) =>
{
    string dir = e.IsTransmit ? "TX" : "RX";
    string hex = BitConverter.ToString(e.Data).Replace("-", " ");
    Console.WriteLine($"  [{e.Timestamp:HH:mm:ss.fff}] {dir}: {hex}");
};

Console.WriteLine("[IKSUNG] 연결 성공!\n");

// ── D. 리더기 정보 조회 ───────────────────────────────────────
Console.WriteLine("── D. GetReaderInfoAsync ──────────────────────────────");
ReaderInfo info = await reader.GetReaderInfoAsync();
Console.WriteLine($"  {info}");
Console.WriteLine();

// ── D. Ping 테스트 ────────────────────────────────────────────
Console.WriteLine("── D. PingAsync ───────────────────────────────────────");
bool alive = await reader.PingAsync(timeoutMs: 500);
Console.WriteLine($"  리더기 응답: {(alive ? "OK" : "TIMEOUT")}");
Console.WriteLine();

// ── D. 버전 읽기 (Raw 패킷 로그 확인용) ──────────────────────
Console.WriteLine("── E. RawPacketReceived (ReadVersion 전송 예시) ───────");
string version = await reader.ReadVersionAsync();
Console.WriteLine($"  Firmware: {version}");
Console.WriteLine();

// ── Raw 명령 직접 전송 (부저 On/Off) ──────────────────────────
Console.WriteLine("── SendRawCommandAsync (부저 1회) ─────────────────────");
// MAJOR_COMMON(0x00) + COMMON_BUZZER(0x11) + data[0x01] = 부저 ON
await reader.SendRawCommandAsync(0x00, 0x11, data: [0x01], timeoutMs: 500);
Console.WriteLine("  부저 명령 전송 완료");
Console.WriteLine();

// ── AutoReconnect 데모 안내 ────────────────────────────────────
Console.WriteLine("── AutoReconnect 데모 ─────────────────────────────────");
Console.WriteLine("  케이블을 뽑으면 ConnectionChanged(false) 이벤트가 발생하고");
Console.WriteLine("  2초마다 자동 재연결을 시도합니다.");
Console.WriteLine("  Ctrl+C 를 눌러 종료하세요.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

// 3초마다 Ping으로 상태 확인
while (!cts.Token.IsCancellationRequested)
{
    try { await Task.Delay(3000, cts.Token); } catch (OperationCanceledException) { break; }

    bool ok = await reader.PingAsync(timeoutMs: 500, ct: cts.Token);
    Console.WriteLine($"  [{DateTime.Now:HH:mm:ss}] Ping: {(ok ? "OK" : "NO RESPONSE")}");
}

// ── F. 명시적 연결 해제 ───────────────────────────────────────
Console.WriteLine("\n── F. DisconnectAsync ─────────────────────────────────");
await reader.DisconnectAsync();
Console.WriteLine("  연결 해제 완료 (AutoReconnect 중단됨)");
Console.WriteLine("\n[IKSUNG] Done.");
