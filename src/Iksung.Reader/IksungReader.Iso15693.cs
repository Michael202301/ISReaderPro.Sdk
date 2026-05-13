using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_ISO15693 (0x04) 명령군 — ISO 15693 활성화, 블록 읽기/쓰기.</summary>
public sealed partial class IksungReader
{
    // ─── ISO 15693 ────────────────────────────────────────────

    /// <summary>ISO 15693 카드를 활성화하고 8바이트 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateIso15693Async(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO15693, Constants.ISO15693_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 단일 블록을 읽는다.</summary>
    /// <param name="blockNo">읽을 블록 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> Iso15693ReadBlockAsync(byte blockNo, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO15693, Constants.ISO15693_SINGLE_BLOCK_READ,
            data: [blockNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 단일 블록을 쓴다.</summary>
    /// <param name="blockNo">쓸 블록 번호</param>
    /// <param name="data">블록 데이터 (카드에 따라 4 또는 8바이트)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task Iso15693WriteBlockAsync(byte blockNo, byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] payload = new byte[1 + data.Length];
        payload[0] = blockNo;
        data.CopyTo(payload, 1);
        await SendCommandAsync(Constants.MAJOR_ISO15693, Constants.ISO15693_SINGLE_BLOCK_WRITE,
            data: payload, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 15693 다중 블록을 읽는다.</summary>
    /// <param name="firstBlock">첫 번째 블록 번호</param>
    /// <param name="blockCount">읽을 블록 수</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> Iso15693ReadMultipleBlocksAsync(byte firstBlock, byte blockCount, int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO15693, Constants.ISO15693_MULTIPLE_BLOCK_READ,
            data: [firstBlock, blockCount], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
