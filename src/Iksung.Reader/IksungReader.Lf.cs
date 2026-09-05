using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_RF125KHZ (0x0C) 명령군 — LF 125 kHz UID 읽기 및 고급 기능.</summary>
public sealed partial class IksungReader
{
    // ─── LF 125 kHz UID 읽기 ─────────────────────────────────

    /// <summary>LF 125 kHz (EM410X) 카드 UID를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLf125KhzUidAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_EM410X_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>LF 125 kHz 고유 ID를 읽는다 (EM410X 이외 형식 포함).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLf125KhzRawUidAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── LF 125 kHz 고급 명령 ────────────────────────────────

    /// <summary>ISO 11784/11785 (FDX-B) 동물 칩을 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>15자리 ISO 11784 번호 포함 바이트 배열</returns>
    public async Task<byte[]> ReadLfIso11784Async(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_ISO11784_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 11784 Low-level Raw Data를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfIso11784LowDataAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_ISO11784_LOW_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>T5577 LF 블록 데이터를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfT5577BlockAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_T5577_BLOCK_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>LF 원시 비트 데이터를 읽는다.</summary>
    /// <param name="bitLength">읽을 비트 길이 (1–255)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfRawBitsAsync(byte bitLength = 64, int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_LOWDATA_READ,
            data: [bitLength], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Temic 칩 블록을 읽는다.</summary>
    /// <param name="readBits">읽을 비트 수</param>
    /// <param name="delayMs">딜레이 (ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfTemicBlockAsync(byte readBits = 0x40, byte delayMs = 0x0A,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        // cmd: readBits, delayMs, cmdBits=0, no command data
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_TEMIC_BLOCK_READ,
            data: [readBits, delayMs, 0x00], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Temic Low-level Raw 비트 데이터를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfTemicLowDataAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_TEMIC_LOWDATA_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>HTRC110 샘플링 시간을 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfHtrcSamplingTimeAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_REG_SAMPLING_TIME_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>HTRC110 샘플링 시간을 자동 조정한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>최적화된 샘플링 시간 값</returns>
    public async Task<byte[]> AutoTuneLfSamplingAsync(int timeoutMs = 3000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_REG_SAMPLING_TIME_AUTO,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>LF 읽기 타이밍 파라미터를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadLfTimingParamsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_RF125KHZ, Constants.RF125_READ_TIMMING_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
