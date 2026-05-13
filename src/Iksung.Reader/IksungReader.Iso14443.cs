using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_ISO14443AB (0x01) 명령군 — ISO 14443-A/B 활성화, Halt, APDU 교환.</summary>
public sealed partial class IksungReader
{
    // ─── ISO 14443-A/B 활성화 ────────────────────────────────

    /// <summary>ISO 14443-A 카드를 활성화한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>카드 UID 바이트 배열</returns>
    public async Task<byte[]> ActivateIso14443aAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443A_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-4A (ISO-DEP) 카드를 활성화한다 (106 kbps).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>UID + ATS(Answer to Select) 바이트 배열</returns>
    public async Task<byte[]> ActivateIso14443_4aAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443_4A_106_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-3A + 4A 순서로 카드를 활성화한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso14443_3a4aAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443_3A_4A_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-B 카드를 활성화한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso14443bAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443B_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-A/B 자동 감지 활성화를 시도한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso14443abAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443AB_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 14443-A 카드를 Halt 상태로 전환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task HaltIso14443aAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_ISO14443AB, Constants.ISO14443A_HALT,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>ISO 14443-B 카드를 Halt 상태로 전환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task HaltIso14443bAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_ISO14443AB, Constants.ISO14443B_HALT,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>
    /// ISO 14443-4 (T=CL) APDU 교환.
    /// 카드를 먼저 <see cref="ActivateIso14443_4aAsync"/> 또는 <see cref="ActivateIso14443_3a4aAsync"/>로 활성화해야 한다.
    /// </summary>
    /// <param name="apdu">C-APDU 바이트 배열</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>R-APDU 바이트 배열</returns>
    public async Task<byte[]> ExchangeApduAsync(byte[] apdu, int timeoutMs = 3000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO14443AB, Constants.ISO14443P4_DATA_EXCHANGE,
            data: apdu, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
