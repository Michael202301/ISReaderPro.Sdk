namespace Iksung.Reader.Internals.Channels;

// Unified operational interface for all communication channels.
// Connection setup (port name, IP, baud rate, etc.) is handled by each concrete class
// before it is handed to IksungReader. This interface covers the run-time operation only.
//
// Events are raised on the background thread that receives data — callers must marshal
// to the UI thread themselves (standard library contract, matches System.IO.Ports behaviour).
internal interface IIksungChannel : IDisposable
{
    bool IsConnected { get; }

    event EventHandler<IksungPacket>? PacketReceived;
    event EventHandler<bool>?         ConnectionChanged;

    void Send(byte[] data);
    void Disconnect();

    // Raw byte I/O for firmware update (XCP / IS-Update). Pauses normal packet parsing.
    IDisposable BeginRawIo();
    bool WriteRaw(byte[] data, int offset, int count);
    int  ReadRaw(byte[] buf, int offset, int count, int timeoutMs);

    // Baud rate change — meaningful only for serial; other channels treat it as no-op.
    void SetBaudRate(int baud);
}
