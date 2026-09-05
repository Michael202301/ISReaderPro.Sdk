using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_MIFARE_ULTRALIGHT (0x03) 명령군 — Mifare Ultralight / NTag 활성화, 페이지 읽기/쓰기, 패스워드 인증.</summary>
public sealed partial class IksungReader
{
    // ─── Mifare Ultralight / NTag ────────────────────────────

    /// <summary>Mifare Ultralight / NTag 카드를 활성화하고 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateMifareUltralightAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.ULC_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Ultralight 페이지(4바이트)를 읽는다.</summary>
    /// <param name="page">읽을 페이지 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>4바이트 페이지 데이터</returns>
    public async Task<byte[]> MifareUltralightReadPageAsync(byte page, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.ULC_BLOCK_READ,
            data: [page], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Ultralight 페이지(4바이트)를 쓴다.</summary>
    /// <param name="page">쓸 페이지 번호</param>
    /// <param name="data">4바이트 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task MifareUltralightWritePageAsync(byte page, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data.Length != 4) throw new ArgumentException("Ultralight 페이지 데이터는 4바이트여야 합니다.", nameof(data));
        byte[] payload = new byte[5];
        payload[0] = page;
        data.CopyTo(payload, 1);
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.ULC_BLOCK_WRITE,
            data: payload, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag 패스워드 인증.</summary>
    /// <param name="password">4바이트 패스워드</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task NTagPasswordAuthAsync(byte[] password, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (password.Length != 4) throw new ArgumentException("NTag 패스워드는 4바이트여야 합니다.", nameof(password));
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_PASSWORD_AUTH,
            data: password, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
