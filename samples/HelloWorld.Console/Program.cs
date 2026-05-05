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
Console.WriteLine($"Connected via : {reader.ConnectedVia}");
Console.WriteLine($"Firmware ver  : {await reader.ReadVersionAsync()}");
Console.WriteLine($"Unique ID     : {BitConverter.ToString(await reader.ReadUniqueIdAsync())}");
