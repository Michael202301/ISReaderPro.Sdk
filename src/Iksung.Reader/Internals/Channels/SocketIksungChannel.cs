using System.IO;
using System.Net;
using System.Net.Sockets;
using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader.Internals.Channels;

internal sealed class SocketIksungChannel : IIksungChannel
{
    private TcpListener?  _listener;
    private CancellationTokenSource? _serverCts;
    private readonly List<TcpClient> _serverClients = [];
    private readonly object _clientsLock = new();

    private TcpClient? _client;
    private CancellationTokenSource? _clientCts;
    private bool _prevConnected;

    public bool IsServerListening  => _listener != null;
    public bool IsServerConnected
    {
        get { lock (_clientsLock) return _listener != null && _serverClients.Count > 0; }
    }
    public bool IsClientConnected => _client?.Connected == true;
    public bool IsConnected       => IsServerConnected || IsClientConnected;

    // Snapshot of connected server-side client endpoints — callers must not cache the list.
    public IReadOnlyList<string> GetConnectedClientEndpoints()
    {
        lock (_clientsLock)
            return _serverClients
                .Select(c => { try { return c.Client?.RemoteEndPoint?.ToString() ?? "?"; } catch { return "?"; } })
                .ToArray();
    }

    public string? SelectedServerClient { get; set; }

    public event EventHandler<IksungPacket>? PacketReceived;
    public event EventHandler<IksungPacket>? TxPacketSent;
    public event EventHandler<bool>?         ConnectionChanged;
    public event EventHandler<string>?       ServerClientAccepted;
    public event EventHandler<string>?       ServerClientDisconnected;

    public Task<bool> StartServerAsync(int port)
    {
        try
        {
            StopServer();
            _serverCts = new CancellationTokenSource();
            _listener  = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            NotifyConnectionChanged();
            _ = AcceptLoopAsync(_serverCts.Token);
            return Task.FromResult(true);
        }
        catch
        {
            _listener  = null;
            _serverCts = null;
            return Task.FromResult(false);
        }
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener!.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                string ep = tcpClient.Client.RemoteEndPoint?.ToString() ?? "?";
                lock (_clientsLock) _serverClients.Add(tcpClient);
                ServerClientAccepted?.Invoke(this, ep);
                NotifyConnectionChanged();
                _ = ReceiveLoopAsync(tcpClient, isServerClient: true, ct, ep);
            }
            catch { break; }
        }
    }

    public void StopServer()
    {
        _serverCts?.Cancel();
        _serverCts = null;
        lock (_clientsLock)
        {
            foreach (var c in _serverClients) try { c.Close(); } catch { }
            _serverClients.Clear();
        }
        try { _listener?.Stop(); } catch { }
        _listener = null;
        SelectedServerClient = null;
        NotifyConnectionChanged();
    }

    public async Task<bool> ConnectClientAsync(string ip, int port, int timeoutMs)
    {
        try
        {
            DisconnectClient();
            _client    = new TcpClient();
            _clientCts = new CancellationTokenSource();
            using var timeout = new CancellationTokenSource(timeoutMs);
            await _client.ConnectAsync(ip, port, timeout.Token).ConfigureAwait(false);
            NotifyConnectionChanged();
            _ = ReceiveLoopAsync(_client, isServerClient: false, _clientCts.Token, null);
            return true;
        }
        catch
        {
            try { _client?.Close(); } catch { }
            _client    = null;
            _clientCts = null;
            return false;
        }
    }

    public void DisconnectClient()
    {
        _clientCts?.Cancel();
        _clientCts = null;
        var c = _client;
        _client = null;
        try { c?.Close(); } catch { }
        NotifyConnectionChanged();
    }

    public void Disconnect()
    {
        _pendingResponse?.TrySetCanceled();
        _pendingResponse = null;
        DisconnectClient();
        StopServer();
    }

    public void SendToClient(byte[] data)
    {
        if (_client?.Connected != true) return;
        try
        {
            _client.GetStream().Write(data, 0, data.Length);
            RaiseTxPacketSent(data);
        }
        catch { DisconnectClient(); }
    }

    public void SendToServer(byte[] data)
    {
        List<TcpClient> clients;
        string? target;
        lock (_clientsLock)
        {
            clients = [.. _serverClients];
            target  = SelectedServerClient;
        }
        if (clients.Count == 0) return;

        if (!string.IsNullOrEmpty(target))
        {
            TcpClient? picked = null;
            foreach (var c in clients)
            {
                string? ep = null;
                try { ep = c.Client?.RemoteEndPoint?.ToString(); } catch { }
                if (ep == target) { picked = c; break; }
            }
            if (picked != null)
            {
                try { picked.GetStream().Write(data, 0, data.Length); RaiseTxPacketSent(data); }
                catch { }
                return;
            }
        }

        bool sent = false;
        var dead  = new List<TcpClient>();
        foreach (var c in clients)
        {
            try { c.GetStream().Write(data, 0, data.Length); sent = true; }
            catch { dead.Add(c); }
        }
        if (dead.Count > 0)
            lock (_clientsLock)
                foreach (var c in dead) { _serverClients.Remove(c); try { c.Close(); } catch { } }

        if (sent) RaiseTxPacketSent(data);
    }

    public void Send(byte[] data)
    {
        if (_client?.Connected == true) { SendToClient(data); return; }
        SendToServer(data);
    }

    public void SocketInit() => Disconnect();

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private TaskCompletionSource<IksungPacket>? _pendingResponse;

    public async Task<byte[]> SendAndReceiveAsync(byte[] request, int timeoutMs, CancellationToken ct = default)
    {
        await _sendLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var tcs = new TaskCompletionSource<IksungPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingResponse = tcs;
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
            _pendingResponse = null;
            _sendLock.Release();
        }
    }

    private volatile bool _rawIoActive;

    public IDisposable BeginRawIo()
    {
        if (!IsConnected)
            throw new InvalidOperationException("Socket 이 연결되지 않았습니다");
        _rawIoActive = true;
        var stream = GetActiveStream();
        if (stream?.DataAvailable == true)
        {
            try
            {
                var drain = new byte[4096];
                while (stream.DataAvailable)
                    if (stream.Read(drain, 0, drain.Length) <= 0) break;
            }
            catch { }
        }
        return new RawIoLease(this);
    }

    public bool WriteRaw(byte[] data, int offset, int count)
    {
        var stream = GetActiveStream();
        if (stream == null) return false;
        try { stream.Write(data, offset, count); stream.Flush(); return true; }
        catch (IOException)               { return false; }
        catch (ObjectDisposedException)   { return false; }
        catch (InvalidOperationException) { return false; }
    }

    public int ReadRaw(byte[] buf, int offset, int count, int timeoutMs)
    {
        var stream = GetActiveStream();
        if (stream == null) return 0;
        int  total    = 0;
        long deadline = Environment.TickCount + timeoutMs;
        while (total < count)
        {
            int remaining = (int)(deadline - Environment.TickCount);
            if (remaining <= 0) return total;
            try
            {
                stream.ReadTimeout = Math.Max(1, remaining);
                int n = stream.Read(buf, offset + total, count - total);
                if (n <= 0) return total;
                total += n;
            }
            catch (IOException)               { return total; }
            catch (ObjectDisposedException)   { return total; }
            catch (InvalidOperationException) { return total; }
        }
        return total;
    }

    public void SetBaudRate(int baud) { }

    private NetworkStream? GetActiveStream()
    {
        if (_client?.Connected == true)
        {
            try { return _client.GetStream(); } catch { return null; }
        }
        TcpClient? tcp = null;
        lock (_clientsLock)
        {
            if (!string.IsNullOrEmpty(SelectedServerClient))
            {
                foreach (var c in _serverClients)
                {
                    string? ep = null;
                    try { ep = c.Client?.RemoteEndPoint?.ToString(); } catch { }
                    if (ep == SelectedServerClient) { tcp = c; break; }
                }
            }
            tcp ??= _serverClients.Count > 0 ? _serverClients[0] : null;
        }
        try { return tcp?.GetStream(); } catch { return null; }
    }

    private sealed class RawIoLease(SocketIksungChannel owner) : IDisposable
    {
        private bool _disposed;
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner._rawIoActive = false;
            var stream = owner.GetActiveStream();
            if (stream?.DataAvailable == true)
            {
                try
                {
                    var drain = new byte[4096];
                    while (stream.DataAvailable)
                        if (stream.Read(drain, 0, drain.Length) <= 0) break;
                }
                catch { }
            }
        }
    }

    private async Task ReceiveLoopAsync(TcpClient tcpClient, bool isServerClient, CancellationToken ct, string? endpoint)
    {
        byte[] readBuf = new byte[4096];
        byte[] rxBuf   = new byte[10240];
        int    rxLen   = 0;
        try
        {
            var stream = tcpClient.GetStream();
            while (!ct.IsCancellationRequested)
            {
                if (_rawIoActive)
                {
                    rxLen = 0;
                    try { await Task.Delay(100, ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                int n;
                using (var iterCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    iterCts.CancelAfter(200);
                    try { n = await stream.ReadAsync(readBuf.AsMemory(0, readBuf.Length), iterCts.Token).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { continue; }
                }
                if (n == 0) break;
                int copy = Math.Min(n, rxBuf.Length - rxLen);
                Array.Copy(readBuf, 0, rxBuf, rxLen, copy);
                rxLen += copy;
                rxLen = TryParsePackets(rxBuf, rxLen);
            }
        }
        catch (OperationCanceledException) { }
        catch { }
        finally
        {
            if (isServerClient)
            {
                lock (_clientsLock) _serverClients.Remove(tcpClient);
                if (!string.IsNullOrEmpty(endpoint))
                {
                    if (SelectedServerClient == endpoint) SelectedServerClient = null;
                    ServerClientDisconnected?.Invoke(this, endpoint);
                }
                NotifyConnectionChanged();
            }
            else
            {
                _client = null;
                NotifyConnectionChanged();
            }
            try { tcpClient.Close(); } catch { }
        }
    }

    private readonly List<byte> _invalidAccumulator = new(256);

    private int TryParsePackets(byte[] buf, int len)
    {
        while (len > 0)
        {
            byte first = buf[0];
            int parsed;

            if      (first == PacketBuilder.STX) parsed = TryParseStandard(buf, len);
            else if (first == 0x02)              parsed = TryParseOldFormat(buf, len);
            else if ((first & 0x10) == 0x10)     parsed = TryParseModbus(buf, len);
            else                                 parsed = -1;

            if (parsed == 0) return len;
            if (parsed < 0)
            {
                _invalidAccumulator.Add(buf[0]);
                len--;
                Array.Copy(buf, 1, buf, 0, len);
                continue;
            }

            FlushInvalidAccumulator();
            len -= parsed;
            if (len > 0) Array.Copy(buf, parsed, buf, 0, len);
        }

        FlushInvalidAccumulator();
        return len;
    }

    private void DeliverPacket(IksungPacket pkt)
    {
        var pending = _pendingResponse;
        if (pending != null && pkt.Protocol != PacketProtocol.Invalid)
            pending.TrySetResult(pkt);
        else
            PacketReceived?.Invoke(this, pkt);
    }

    private void FlushInvalidAccumulator()
    {
        if (_invalidAccumulator.Count == 0) return;
        var data = _invalidAccumulator.ToArray();
        _invalidAccumulator.Clear();
        DeliverPacket(new IksungPacket(0, 0, 0, [], lowData: data, protocol: PacketProtocol.Invalid));
    }

    private int TryParseStandard(byte[] buf, int len)
    {
        const int Header = 6, Trailer = 2;
        if (len < Header + Trailer) return 0;
        int dataLen  = (buf[4] << 8) | buf[5];
        int totalLen = Header + dataLen + Trailer;
        if (totalLen > buf.Length) return -1;
        if (len < totalLen)         return 0;

        if (buf[totalLen - 1] != PacketBuilder.ETX)                            return -1;
        if (PacketBuilder.CalcChecksum(buf, 1, 5 + dataLen) != buf[6 + dataLen]) return -1;

        byte cmd1 = buf[1], cmd2 = buf[2], state = buf[3];
        byte[] data = new byte[dataLen];
        if (dataLen > 0) Array.Copy(buf, 6, data, 0, dataLen);
        byte[] raw = new byte[totalLen];
        Array.Copy(buf, 0, raw, 0, totalLen);

        DeliverPacket(new IksungPacket(cmd1, cmd2, state, data, raw, PacketProtocol.Standard));
        return totalLen;
    }

    private int TryParseOldFormat(byte[] buf, int len)
    {
        const int Header = 5, Trailer = 2;
        if (len < Header + Trailer) return 0;
        int dataLen  = (buf[3] << 8) | buf[4];
        int totalLen = Header + dataLen + Trailer;
        if (totalLen > buf.Length) return -1;
        if (len < totalLen)         return 0;

        if (buf[totalLen - 1] != PacketBuilder.ETX)                            return -1;
        if (PacketBuilder.CalcChecksum(buf, 1, 4 + dataLen) != buf[5 + dataLen]) return -1;

        byte cmd = buf[1], state = buf[2];
        byte[] data = new byte[dataLen];
        if (dataLen > 0) Array.Copy(buf, 5, data, 0, dataLen);
        byte[] raw = new byte[totalLen];
        Array.Copy(buf, 0, raw, 0, totalLen);

        DeliverPacket(new IksungPacket(0, cmd, state, data, raw, PacketProtocol.OldFormat));
        return totalLen;
    }

    private int TryParseModbus(byte[] buf, int len)
    {
        if (len < 5) return 0;
        byte func = buf[1];
        if (func != 0x03) return -1;
        int byteCount = buf[2];
        int totalLen  = 3 + byteCount + 2;
        if (totalLen > buf.Length) return -1;
        if (len < totalLen)         return 0;

        ushort calcCrc  = PacketBuilder.ModbusCrc16(buf, 3 + byteCount);
        ushort frameCrc = (ushort)((buf[3 + byteCount] << 8) | buf[3 + byteCount + 1]);
        if (calcCrc != frameCrc) return -1;

        byte[] data = new byte[byteCount];
        if (byteCount > 0) Array.Copy(buf, 3, data, 0, byteCount);
        byte[] raw = new byte[totalLen];
        Array.Copy(buf, 0, raw, 0, totalLen);

        DeliverPacket(new IksungPacket(buf[0], func, 0, data, raw, PacketProtocol.ModbusRtu));
        return totalLen;
    }

    private void RaiseTxPacketSent(byte[] data)
    {
        if (data.Length < 7 || data[0] != PacketBuilder.STX) return;
        byte cmd1   = data[1], cmd2 = data[2];
        int dataLen = (data[3] << 8) | data[4];
        byte[] payload = new byte[dataLen];
        if (dataLen > 0 && data.Length >= 5 + dataLen)
            Array.Copy(data, 5, payload, 0, dataLen);
        TxPacketSent?.Invoke(this, new IksungPacket(cmd1, cmd2, 0, payload));
    }

    private void NotifyConnectionChanged()
    {
        bool now = IsConnected;
        if (now == _prevConnected) return;
        _prevConnected = now;
        ConnectionChanged?.Invoke(this, now);
    }

    public void Dispose() => Disconnect();
}
