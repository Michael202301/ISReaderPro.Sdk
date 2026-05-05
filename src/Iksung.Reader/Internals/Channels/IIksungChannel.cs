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

    // Fired for unsolicited packets (AutoRead tag arrivals) only.
    // Request-response commands use SendAndReceiveAsync instead.
    event EventHandler<IksungPacket>? PacketReceived;
    event EventHandler<bool>?         ConnectionChanged;

    // Fire-and-forget send (for commands that don't need a response, e.g. buzzer).
    void Send(byte[] data);

    // Send a request packet and wait up to timeoutMs for the matching response.
    // Returns the Data payload of the response packet.
    // Throws IksungTimeoutException when no response arrives within timeoutMs.
    // Throws IksungProtocolException when the response indicates STATE_FAIL.
    Task<byte[]> SendAndReceiveAsync(byte[] request, int timeoutMs, CancellationToken ct = default);

    void Disconnect();

    // Raw byte I/O for firmware update (XCP / IS-Update). Pauses normal packet parsing.
    IDisposable BeginRawIo();
    bool WriteRaw(byte[] data, int offset, int count);
    int  ReadRaw(byte[] buf, int offset, int count, int timeoutMs);

    // Baud rate change — meaningful only for serial; other channels treat it as no-op.
    void SetBaudRate(int baud);
}
