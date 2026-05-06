using System;
using System.Threading.Tasks;
using Iksung.Reader;

namespace HelloWorld.Console.Net4x
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                System.Console.WriteLine("Usage: HelloWorld.Console <portName> [baudRate]");
                System.Console.WriteLine("  Windows : HelloWorld.Console COM3 115200");
                System.Console.WriteLine("  Linux   : HelloWorld.Console /dev/ttyUSB0 115200");
                System.Console.WriteLine("  Mac     : HelloWorld.Console /dev/tty.usbserial-XXXX 115200");
                return;
            }

            string portName = args[0];
            int baudRate = args.Length >= 2 ? int.Parse(args[1]) : 115200;

            System.Console.WriteLine($"Connecting to {portName} @ {baudRate} bps ...");

            var reader = await IksungReader.ConnectSerialAsync(portName, baudRate);
            try
            {
                System.Console.WriteLine($"Connected via : {reader.ConnectedVia}");
                System.Console.WriteLine($"Firmware ver  : {await reader.ReadVersionAsync()}");
                System.Console.WriteLine($"Unique ID     : {BitConverter.ToString(await reader.ReadUniqueIdAsync())}");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }
    }
}
