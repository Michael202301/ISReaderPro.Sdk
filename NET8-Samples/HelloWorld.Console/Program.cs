/*
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

if (args.Length < 1)
{
    Console.WriteLine("Usage: HelloWorld.Console <portName> [baudRate]");
    Console.WriteLine("  Windows : HelloWorld.Console COM3 115200");
    Console.WriteLine("  Linux   : HelloWorld.Console /dev/ttyUSB0 115200");
    Console.WriteLine("  Mac     : HelloWorld.Console /dev/tty.usbserial-XXXX 115200");
    return;
}

string portName = args[0];
int    baudRate = args.Length >= 2 ? int.Parse(args[1]) : 115200;

Console.WriteLine($"Connecting to {portName} @ {baudRate} bps …");

await using var reader = await IksungReader.ConnectSerialAsync(portName, baudRate);
// ── 연결 확인 ─────────────────────────────────────────────────
Console.WriteLine("[IKSUNG] 리더기 응답 확인 중...");
if (!await reader.PingAsync(timeoutMs: 500))
{
    Console.WriteLine($"[ERROR] 리더기가 응답하지 않습니다. (포트: {portName})");
    Console.WriteLine("  - 리더기 전원을 확인하세요.");
    Console.WriteLine("  - 케이블 연결을 확인하세요.");
    return;
}
Console.WriteLine($"Connected via : {reader.ConnectedVia}");
Console.WriteLine($"Firmware ver  : {await reader.ReadVersionAsync()}");
Console.WriteLine($"Unique ID     : {BitConverter.ToString(await reader.ReadUniqueIdAsync())}");
