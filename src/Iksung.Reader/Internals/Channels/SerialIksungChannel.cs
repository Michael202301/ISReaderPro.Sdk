using System.IO;
using System.IO.Ports;
using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader.Internals.Channels;

internal sealed class SerialIksungChannel : IIksungChannel
{
    private SerialPort? _port;
    private readonly string _portName;
    private readonly int    _baudRate;
    private readonly int    _dataBits;
    private readonly StopBits  _stopBits;
    private readonly Parity    _parity;
    private readonly Handshake _handshake;

    private readonly byte[] _rxBuf = new byte[10240];
    private int _rxLen;
    private readonly object _rxLock = new();
    private long _lastRxTick;

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private (TaskCompletionSource<IksungPacket> Tcs, byte Cmd1, byte Cmd2)? _pending;

    // Guard flag: prevents DataReceived from touching the port while Close/Dispose is running.
    // .NET Framework SerialPort has a well-known race: if DataReceived is executing on a
    // ThreadPool thread while Close() is called on another thread, the native handle becomes
    // invalid and causes 0xC0000005 (ACCESS_VIOLATION). Setting _closing = true and checking
    // it at the top of OnDataReceived prevents the handler from using a closing port.
    private volatile bool _closing;

    public bool IsConnected => _port?.IsOpen == true;

    public event EventHandler<IksungPacket>? PacketReceived;
    public event EventHandler<IksungPacket>? TxPacketSent;
    public event EventHandler<bool>?         ConnectionChanged;
    public event EventHandler<byte[]>?       RawDataReceived;

    public SerialIksungChannel(string portName, int baudRate,
        int dataBits = 8, StopBits stopBits = StopBits.One,
        Parity parity = Parity.None, Handshake handshake = Handshake.None)
    {
        _portName  = portName;
        _baudRate  = baudRate;
        _dataBits  = dataBits;
        _stopBits  = stopBits;
        _parity    = parity;
        _handshake = handshake;
    }

    public IEnumerable<string> GetAvailablePorts() => SerialPort.GetPortNames();

    // Connects using the parameters given to the constructor.
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Configure(_portName, _baudRate, _dataBits, _stopBits, _parity, _handshake);
        bool ok = await OpenAsync().ConfigureAwait(false);
        if (!ok) throw new IksungException($"'{_portName}' 포트를 열 수 없습니다.");
    }

    public void Configure(string port, int baudRate, int dataBits,
                          StopBits stopBits, Parity parity, Handshake handshake)
    {
        if (_port?.IsOpen == true) Disconnect();
        _port = new SerialPort(port, baudRate, parity, dataBits, stopBits)
        {
            Handshake       = handshake,
            DtrEnable       = true,
            DiscardNull     = false,
            ReadBufferSize  = 10240,
            WriteBufferSize = 10240,
        };
        _port.DataReceived  += OnDataReceived;
        _port.ErrorReceived += OnErrorReceived;
        lock (_rxLock) _rxLen = 0;
    }

    private async Task<bool> OpenAsync()
    {
        return await Task.Run(() =>
        {
            if (_port == null) return false;
            try
            {
                if (_port.IsOpen) _port.Close();
                _port.Open();
                _port.ReadTimeout = 500;
                ConnectionChanged?.Invoke(this, true);
                return true;
            }
            catch
            {
                _port?.Dispose();
                _port = null;
                return false;
            }
        }).ConfigureAwait(false);
    }

    public void Disconnect()
    {
        if (_port == null) return;
        _closing = true;                            // signal OnDataReceived to bail out early
        _port.DataReceived  -= OnDataReceived;
        _port.ErrorReceived -= OnErrorReceived;
        // Brief pause so any DataReceived handler already running on the ThreadPool has time
        // to observe _closing = true and return before we pull the native handle out from under it.
        Thread.Sleep(50);
        try { _port.Close();   } catch { }
        try { _port.Dispose(); } catch { }
        _port    = null;
        _closing = false;
        _pending?.Tcs.TrySetCanceled();
        _pending = null;
        ConnectionChanged?.Invoke(this, false);
    }

    public void Send(byte[] data)
    {
        if (_port?.IsOpen != true) return;
        try { _port.Write(data, 0, data.Length); }
        catch (IOException)               { HandleDisconnect(); return; }
        catch (InvalidOperationException) { HandleDisconnect(); return; }
        RaiseTxEvent(data);
    }

    public async Task<byte[]> SendAndReceiveAsync(byte[] request, int timeoutMs, CancellationToken ct = default)
    {
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var tcs   = new TaskCompletionSource<IksungPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            byte rCmd1 = request.Length > 1 ? request[1] : (byte)0;
            byte rCmd2 = request.Length > 2 ? request[2] : (byte)0;
            _pending = (tcs, rCmd1, rCmd2);
            Send(request);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(timeoutMs);
            using (linked.Token.Register(() => tcs.TrySetCanceled(linked.Token)))
            {
                IksungPacket pkt;
                try   { pkt = await tcs.Task.ConfigureAwait(false); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new IksungTimeoutException($"응답 대기 시간 초과 ({timeoutMs} ms)");
                }

                if (pkt.State == Constants.STATE_FAIL)
                    throw new IksungProtocolException($"장치가 오류 응답 반환 (cmd1=0x{pkt.Cmd1:X2} cmd2=0x{pkt.Cmd2:X2})");

                return pkt.Data;
            }
        }
        finally
        {
            _pending = null;
            _sendLock.Release();
        }
    }

    // ─── Raw I/O (bootloader) ───────────────────────────────

    private volatile bool _rawIoActive;

    public IDisposable BeginRawIo()
    {
        if (_port == null) throw new InvalidOperationException("Serial port 가 열려있지 않습니다");
        _rawIoActive = true;
        try { _port.DiscardInBuffer();  } catch { }
        try { _port.DiscardOutBuffer(); } catch { }
        lock (_rxLock) _rxLen = 0;
        return new RawIoLease(this);
    }

    public bool WriteRaw(byte[] data, int offset, int count)
    {
        if (_port?.IsOpen != true) return false;
        try
        {
            _port.Write(data, offset, count);
            return true;
        }
        catch (IOException)               { return false; }
        catch (InvalidOperationException) { return false; }
    }

    public int ReadRaw(byte[] buf, int offset, int count, int timeoutMs)
    {
        if (_port?.IsOpen != true) return 0;
        var start     = Environment.TickCount;
        int totalRead = 0;
        while (totalRead < count)
        {
            int avail;
            try { avail = _port.BytesToRead; } catch { return totalRead; }
            if (avail > 0)
            {
                int toRead = Math.Min(avail, count - totalRead);
                try
                {
                    int n = _port.Read(buf, offset + totalRead, toRead);
                    if (n > 0) totalRead += n;
                }
                catch (TimeoutException)          { }
                catch (IOException)               { return totalRead; }
                catch (InvalidOperationException) { return totalRead; }
            }
            else
            {
                if (Environment.TickCount - start >= timeoutMs) return totalRead;
                Thread.Sleep(1);
            }
        }
        return totalRead;
    }

    public void SetBaudRate(int baud)
    {
        if (_port == null) return;
        try
        {
            _port.BaudRate = baud;
            try { _port.DiscardInBuffer();  } catch { }
            try { _port.DiscardOutBuffer(); } catch { }
        }
        catch { }
    }

    // ─── Internals ──────────────────────────────────────────

    private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        if (_port?.IsOpen != true) HandleDisconnect();
    }

    private void HandleDisconnect()
    {
        var port = _port;
        if (port == null) return;
        _closing = true;                            // guard before touching the port
        _port    = null;
        try { port.DataReceived  -= OnDataReceived; }  catch { }
        try { port.ErrorReceived -= OnErrorReceived; } catch { }
        Thread.Sleep(50);
        try { port.Close();   } catch { }
        try { port.Dispose(); } catch { }
        _closing = false;
        _pending?.Tcs.TrySetCanceled();
        _pending = null;
        ConnectionChanged?.Invoke(this, false);
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_closing) return;           // port is being closed — bail out immediately
        var port = _port;
        if (port == null) return;
        if (_rawIoActive) return;

        int available;
        try { available = port.BytesToRead; }
        catch (IOException)               { HandleDisconnect(); return; }
        catch (InvalidOperationException) { HandleDisconnect(); return; }

        if (available <= 0) return;
        byte[] incoming = new byte[available];
        try { port.Read(incoming, 0, available); }
        catch (TimeoutException)          { return; }
        catch (IOException)               { HandleDisconnect(); return; }
        catch (InvalidOperationException) { HandleDisconnect(); return; }

        lock (_rxLock)
        {
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (_lastRxTick > 0 && now > _lastRxTick + 200) _rxLen = 0;
            _lastRxTick = now;
            foreach (byte b in incoming)
            {
                if (_rxLen >= _rxBuf.Length) _rxLen = 0;
                _rxBuf[_rxLen++] = b;
            }
            TryParsePackets();
        }
    }

    private void DeliverPacket(IksungPacket pkt)
    {
        if (pkt.LowData.Length > 0)
            RawDataReceived?.Invoke(this, pkt.LowData);

        var p = _pending;
        if (p != null && pkt.Protocol != PacketProtocol.Invalid
                      && pkt.Cmd1 == p.Value.Cmd1
                      && (pkt.Cmd2 & ~Constants.BUZZER_FLAG) == p.Value.Cmd2)
            p.Value.Tcs.TrySetResult(pkt);
        else
            PacketReceived?.Invoke(this, pkt);
    }

    public async Task<bool> ReconnectAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            Configure(_portName, _baudRate, _dataBits, _stopBits, _parity, _handshake);
            return await OpenAsync().ConfigureAwait(false);
        }
        catch { return false; }
    }

    private readonly List<byte> _invalidAccumulator = new(256);

    private void TryParsePackets()
    {
        while (_rxLen > 0)
        {
            byte first = _rxBuf[0];
            int parsed;

            if      (first == PacketBuilder.STX) parsed = TryParseStandard();
            else if (first == 0x02)              parsed = TryParseOldFormat();
            else if ((first & 0x10) == 0x10)     parsed = TryParseModbus();
            else                                 parsed = -1;

            if (parsed == 0) return;
            if (parsed < 0)
            {
                _invalidAccumulator.Add(_rxBuf[0]);
                ConsumeFront(1);
                continue;
            }

            FlushInvalidAccumulator();
            ConsumeFront(parsed);
        }
        FlushInvalidAccumulator();
    }

    private void FlushInvalidAccumulator()
    {
        if (_invalidAccumulator.Count == 0) return;
        var data = _invalidAccumulator.ToArray();
        _invalidAccumulator.Clear();
        DeliverPacket(new IksungPacket(0, 0, 0, [], lowData: data, protocol: PacketProtocol.Invalid));
    }

    private int TryParseStandard()
    {
        const int Header = 6, Trailer = 2;
        if (_rxLen < Header + Trailer) return 0;
        int dataLen  = (_rxBuf[4] << 8) | _rxBuf[5];
        int totalLen = Header + dataLen + Trailer;
        if (totalLen > _rxBuf.Length) return -1;
        if (_rxLen < totalLen)         return 0;

        if (_rxBuf[totalLen - 1] != PacketBuilder.ETX)                           return -1;
        if (PacketBuilder.CalcChecksum(_rxBuf, 1, 5 + dataLen) != _rxBuf[6 + dataLen]) return -1;

        byte cmd1 = _rxBuf[1], cmd2 = _rxBuf[2], state = _rxBuf[3];
        byte[] data = new byte[dataLen];
        if (dataLen > 0) Array.Copy(_rxBuf, 6, data, 0, dataLen);
        byte[] raw = new byte[totalLen];
        Array.Copy(_rxBuf, 0, raw, 0, totalLen);

        DeliverPacket(new IksungPacket(cmd1, cmd2, state, data, raw, PacketProtocol.Standard));
        return totalLen;
    }

    private int TryParseOldFormat()
    {
        const int Header = 5, Trailer = 2;
        if (_rxLen < Header + Trailer) return 0;
        int dataLen  = (_rxBuf[3] << 8) | _rxBuf[4];
        int totalLen = Header + dataLen + Trailer;
        if (totalLen > _rxBuf.Length) return -1;
        if (_rxLen < totalLen)         return 0;

        if (_rxBuf[totalLen - 1] != PacketBuilder.ETX)                           return -1;
        if (PacketBuilder.CalcChecksum(_rxBuf, 1, 4 + dataLen) != _rxBuf[5 + dataLen]) return -1;

        byte cmd = _rxBuf[1], state = _rxBuf[2];
        byte[] data = new byte[dataLen];
        if (dataLen > 0) Array.Copy(_rxBuf, 5, data, 0, dataLen);
        byte[] raw = new byte[totalLen];
        Array.Copy(_rxBuf, 0, raw, 0, totalLen);

        DeliverPacket(new IksungPacket(0, cmd, state, data, raw, PacketProtocol.OldFormat));
        return totalLen;
    }

    private int TryParseModbus()
    {
        if (_rxLen < 5) return 0;
        byte func = _rxBuf[1];
        if (func != 0x03)             return -1;
        int byteCount = _rxBuf[2];
        int totalLen  = 3 + byteCount + 2;
        if (totalLen > _rxBuf.Length) return -1;
        if (_rxLen < totalLen)         return 0;

        ushort calcCrc  = PacketBuilder.ModbusCrc16(_rxBuf, 3 + byteCount);
        ushort frameCrc = (ushort)((_rxBuf[3 + byteCount] << 8) | _rxBuf[3 + byteCount + 1]);
        if (calcCrc != frameCrc)      return -1;

        byte[] data = new byte[byteCount];
        if (byteCount > 0) Array.Copy(_rxBuf, 3, data, 0, byteCount);
        byte[] raw = new byte[totalLen];
        Array.Copy(_rxBuf, 0, raw, 0, totalLen);

        DeliverPacket(new IksungPacket(_rxBuf[0], func, 0, data, raw, PacketProtocol.ModbusRtu));
        return totalLen;
    }

    private void ConsumeFront(int count)
    {
        if (count >= _rxLen) { _rxLen = 0; return; }
        _rxLen -= count;
        Array.Copy(_rxBuf, count, _rxBuf, 0, _rxLen);
    }

    private void RaiseTxEvent(byte[] data)
    {
        if (data.Length < 7 || data[0] != PacketBuilder.STX) return;
        byte cmd1   = data[1], cmd2 = data[2];
        int dataLen = (data[3] << 8) | data[4];
        byte[] payload = new byte[dataLen];
        if (dataLen > 0 && data.Length >= 5 + dataLen)
            Array.Copy(data, 5, payload, 0, dataLen);
        TxPacketSent?.Invoke(this, new IksungPacket(cmd1, cmd2, 0, payload, lowData: data));
    }

    private sealed class RawIoLease(SerialIksungChannel owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner._rawIoActive = false;
            try { owner._port?.DiscardInBuffer();  } catch { }
            try { owner._port?.DiscardOutBuffer(); } catch { }
            lock (owner._rxLock) owner._rxLen = 0;
        }
    }

    public void Dispose() => Disconnect();
}
