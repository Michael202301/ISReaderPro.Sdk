using System.Text;
using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>
/// 시스템 레벨 기능 — 연결 생명주기, 진단, 재연결, Raw 패킷 모니터링.
/// </summary>
public sealed partial class IksungReader
{
    // ─── Fields ─────────────────────────────────────────────

    private volatile bool _intentionalDisconnect;
    private IksungReaderOptions _options = new();

    // ─── Properties ─────────────────────────────────────────

    /// <summary>현재 적용된 연결 옵션.</summary>
    public IksungReaderOptions Options => _options;

    // ─── Events ─────────────────────────────────────────────

    /// <summary>
    /// 연결 상태가 변경될 때 발생한다.
    /// <c>true</c> = 연결됨, <c>false</c> = 끊어짐.
    /// 이벤트 핸들러는 백그라운드 스레드에서 호출되므로 UI 접근 시 디스패치가 필요하다.
    /// </summary>
    public event EventHandler<bool>? ConnectionChanged;

    /// <summary>
    /// <see cref="IksungReaderOptions.LogRawPackets"/>가 <c>true</c>일 때,
    /// 송신(TX) 및 수신(RX) raw 바이트 패킷마다 발생한다.
    /// 이벤트 핸들러는 백그라운드 스레드에서 호출된다.
    /// </summary>
    public event EventHandler<RawPacketEventArgs>? RawPacketReceived;

    // ─── Connection lifecycle ────────────────────────────────

    /// <summary>
    /// 옵션을 적용한다. 연결 중에도 즉시 반영된다.
    /// </summary>
    /// <param name="options">적용할 옵션. null 불가.</param>
    public void ApplyOptions(IksungReaderOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 리더기 연결을 명시적으로 해제한다.
    /// <see cref="IksungReaderOptions.AutoReconnect"/>가 <c>true</c>여도
    /// 이 메서드 호출 후에는 자동 재연결하지 않는다.
    /// </summary>
    public async Task DisconnectAsync()
    {
        ThrowIfDisposed();
        _intentionalDisconnect = true;
        await Task.Run(_channel.Disconnect).ConfigureAwait(false);
    }

    // ─── Diagnostics ─────────────────────────────────────────

    /// <summary>
    /// 리더기에 Version 요청을 보내 응답 여부를 확인한다.
    /// 연결되어 있지 않거나 타임아웃이면 예외 없이 <c>false</c>를 반환한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms). -1 이면 <see cref="IksungReaderOptions.DefaultTimeoutMs"/> 사용.</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<bool> PingAsync(int timeoutMs = -1, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (!_channel.IsConnected) return false;
        int ms = timeoutMs < 0 ? _options.DefaultTimeoutMs : timeoutMs;
        try
        {
            await SendCommandAsync(
                Constants.MAJOR_COMMON, Constants.COMMON_VERSION,
                timeoutMs: ms, ct: ct).ConfigureAwait(false);
            return true;
        }
        catch (IksungTimeoutException)         { return false; }
        catch (IksungProtocolException)        { return false; }
        catch (ChannelDisconnectedException)   { return false; }
        catch (OperationCanceledException)     { return false; }
    }

    /// <summary>
    /// 리더기의 시스템 정보(펌웨어 버전, 채널, 포트)를 조회한다.
    /// 타임아웃 또는 연결 끊김 시 <see cref="ReaderInfo.IsConnected"/> = <c>false</c>인 구조체를 반환한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms). -1 이면 <see cref="IksungReaderOptions.DefaultTimeoutMs"/> 사용.</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<ReaderInfo> GetReaderInfoAsync(int timeoutMs = -1, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        int ms = timeoutMs < 0 ? _options.DefaultTimeoutMs : timeoutMs;
        if (!_channel.IsConnected)
            return new ReaderInfo(string.Empty, ConnectedVia, false, _portOrAddress);

        try
        {
            byte[] resp = await SendCommandAsync(
                Constants.MAJOR_COMMON, Constants.COMMON_VERSION,
                timeoutMs: ms, ct: ct).ConfigureAwait(false);
            string version = Encoding.ASCII.GetString(resp).TrimEnd('\0').Trim();
            return new ReaderInfo(version, ConnectedVia, true, _portOrAddress);
        }
        catch
        {
            return new ReaderInfo(string.Empty, ConnectedVia, false, _portOrAddress);
        }
    }

    // ─── Internal event handlers ─────────────────────────────

    private void OnChannelConnectionChanged(object? sender, bool isConnected)
    {
        if (isConnected)
            _intentionalDisconnect = false;   // 재연결 성공 시 플래그 초기화

        ConnectionChanged?.Invoke(this, isConnected);

        if (!isConnected && _options.AutoReconnect && !_intentionalDisconnect && !_disposed)
            _ = RunAutoReconnectAsync();
    }

    private void OnChannelRawDataReceived(object? sender, byte[] rawBytes)
    {
        if (_options.LogRawPackets)
            RawPacketReceived?.Invoke(this, new RawPacketEventArgs(rawBytes, isTransmit: false));
    }

    // ─── AutoReconnect ───────────────────────────────────────

    private async Task RunAutoReconnectAsync()
    {
        while (!_disposed && _options.AutoReconnect && !_intentionalDisconnect)
        {
            try
            {
                await Task.Delay(_options.ReconnectDelayMs).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return; }

            if (_disposed || _intentionalDisconnect || _channel.IsConnected) return;

            bool ok = await _channel.ReconnectAsync().ConfigureAwait(false);
            if (ok) return;   // ConnectionChanged(true) fires → _intentionalDisconnect reset
        }
    }
}
