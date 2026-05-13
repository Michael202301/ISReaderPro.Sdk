using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_MIFARE (0x02) 명령군 — Mifare Classic 활성화, 인증, 읽기/쓰기.</summary>
public sealed partial class IksungReader
{
    // ─── Mifare Classic ──────────────────────────────────────

    /// <summary>Mifare Classic 카드를 활성화하고 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateMifareAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE, Constants.MIFARE_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 블록 인증.</summary>
    /// <param name="blockNo">인증할 블록 번호 (0–255)</param>
    /// <param name="keyType"><see cref="MifareKeyType.KeyA"/> 또는 <see cref="MifareKeyType.KeyB"/></param>
    /// <param name="key">6바이트 인증 키</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task MifareAuthenticateAsync(
        byte blockNo, MifareKeyType keyType, byte[] key,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (key.Length != 6) throw new ArgumentException("Mifare 키는 6바이트여야 합니다.", nameof(key));
        byte[] data = new byte[8];
        data[0] = blockNo;
        data[1] = (byte)keyType;
        key.CopyTo(data, 2);
        await SendCommandAsync(Constants.MAJOR_MIFARE, Constants.MIFARE_AUTHENTICATE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 블록(16바이트)을 읽는다.</summary>
    /// <param name="blockNo">읽을 블록 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>16바이트 블록 데이터</returns>
    public async Task<byte[]> MifareReadBlockAsync(byte blockNo, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE, Constants.MIFARE_BLOCK_READ,
            data: [blockNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 블록(16바이트)을 쓴다.</summary>
    /// <param name="blockNo">쓸 블록 번호</param>
    /// <param name="data">16바이트 블록 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task MifareWriteBlockAsync(byte blockNo, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data.Length != 16) throw new ArgumentException("Mifare 블록 데이터는 16바이트여야 합니다.", nameof(data));
        byte[] payload = new byte[17];
        payload[0] = blockNo;
        data.CopyTo(payload, 1);
        await SendCommandAsync(Constants.MAJOR_MIFARE, Constants.MIFARE_BLOCK_WRITE,
            data: payload, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Mifare Classic 섹터(64바이트) 전체를 읽는다.</summary>
    /// <param name="sectorNo">섹터 번호 (0–15)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> MifareReadSectorAsync(byte sectorNo, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE, Constants.MIFARE_SECTOR_READ,
            data: [sectorNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
