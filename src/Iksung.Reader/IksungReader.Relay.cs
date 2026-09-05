using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>Relay 보드 제어 관련 명령 (MAJOR_RELAY = 0x22, MAJOR_RELAY_CONFIG = 0x23).</summary>
public sealed partial class IksungReader
{
    // ─── 입력(Input) 읽기 ────────────────────────────────────

    /// <summary>전체 Input 채널 상태를 한 번에 읽는다 (5채널 비트마스크).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>1바이트 비트마스크 (bit0=Input1, bit4=Input5)</returns>
    public async Task<byte[]> RelayReadAllInputsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RELAY, Constants.RELAY_INPUT_READ_ALL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>특정 Input 채널 상태를 읽는다.</summary>
    /// <param name="inputNo">Input 채널 번호 (1–5)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>1바이트 상태 (0=Low, 1=High)</returns>
    public async Task<byte[]> RelayReadInputAsync(byte inputNo,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (inputNo is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(inputNo), "Input 번호는 1–5 사이여야 합니다.");
        byte cmd2 = (byte)(Constants.RELAY_INPUT_READ_1 + (inputNo - 1));
        return await SendCommandAsync(
            Constants.MAJOR_RELAY, cmd2,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 출력(Relay) 읽기 ────────────────────────────────────

    /// <summary>전체 Relay 출력 상태를 한 번에 읽는다 (8채널 비트마스크).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>1바이트 비트마스크 (bit0=Relay1, bit7=Relay8)</returns>
    public async Task<byte[]> RelayReadAllOutputsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RELAY, Constants.RELAY_RELAY_READ_ALL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>특정 Relay 채널 상태를 읽는다.</summary>
    /// <param name="relayNo">Relay 채널 번호 (1–8)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> RelayReadOutputAsync(byte relayNo,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (relayNo is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(relayNo), "Relay 번호는 1–8 사이여야 합니다.");
        byte cmd2 = (byte)(Constants.RELAY_RELAY_READ_1 + (relayNo - 1));
        return await SendCommandAsync(
            Constants.MAJOR_RELAY, cmd2,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 출력(Relay) 쓰기 ────────────────────────────────────

    /// <summary>전체 Relay 출력 상태를 비트마스크로 한 번에 설정한다.</summary>
    /// <param name="mask">8비트 마스크 (bit0=Relay1, bit7=Relay8; 1=On, 0=Off)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task RelayWriteAllOutputsAsync(byte mask,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_RELAY, Constants.RELAY_RELAY_WRITE_ALL,
            data: [mask], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>특정 Relay 채널 하나를 On 또는 Off로 설정한다.</summary>
    /// <param name="relayNo">Relay 채널 번호 (1–8)</param>
    /// <param name="on">true=On, false=Off</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task RelayWriteOutputAsync(byte relayNo, bool on,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (relayNo is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(relayNo), "Relay 번호는 1–8 사이여야 합니다.");
        byte cmd2 = (byte)(Constants.RELAY_RELAY_WRITE_1 + (relayNo - 1));
        await SendCommandAsync(
            Constants.MAJOR_RELAY, cmd2,
            data: [on ? (byte)1 : (byte)0], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>모든 Relay를 Off(0)로 설정한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task RelayAllOffAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => RelayWriteAllOutputsAsync(0x00, timeoutMs, ct);

    /// <summary>모든 Relay를 On(1)으로 설정한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task RelayAllOnAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => RelayWriteAllOutputsAsync(0xFF, timeoutMs, ct);

    // ─── Relay 설정 ──────────────────────────────────────────

    /// <summary>Auto-Off Relay 시간을 설정한다.</summary>
    /// <param name="relayNo">Relay 채널 번호 (1–8)</param>
    /// <param name="timeMs">자동 Off 지연 시간 (ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task RelaySetAutoOffTimeAsync(byte relayNo, ushort timeMs,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (relayNo is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(relayNo), "Relay 번호는 1–8 사이여야 합니다.");
        byte[] data = new byte[3];
        data[0] = relayNo;
        data[1] = (byte)(timeMs >> 8);
        data[2] = (byte)(timeMs & 0xFF);
        await SendCommandAsync(
            Constants.MAJOR_RELAY, Constants.RELAY_AUTO_OFF_RELAY_TIME_WRITE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 유틸리티 ────────────────────────────────────────────

    /// <summary>Input 비트마스크에서 특정 채널 상태를 추출한다.</summary>
    /// <param name="mask">RelayReadAllInputsAsync에서 반환된 마스크 바이트</param>
    /// <param name="inputNo">Input 채널 번호 (1–5)</param>
    /// <returns>해당 채널이 High(활성)이면 true</returns>
    public static bool GetInputState(byte mask, byte inputNo)
        => inputNo is >= 1 and <= 5 && (mask & (1 << (inputNo - 1))) != 0;

    /// <summary>Relay 비트마스크에서 특정 채널 상태를 추출한다.</summary>
    /// <param name="mask">RelayReadAllOutputsAsync에서 반환된 마스크 바이트</param>
    /// <param name="relayNo">Relay 채널 번호 (1–8)</param>
    /// <returns>해당 채널이 On이면 true</returns>
    public static bool GetRelayState(byte mask, byte relayNo)
        => relayNo is >= 1 and <= 8 && (mask & (1 << (relayNo - 1))) != 0;
}
