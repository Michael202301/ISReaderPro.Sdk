using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>Mifare NTag 213/215/216 추가 명령 (MAJOR_MIFARE_ULTRALIGHT = 0x05).</summary>
public sealed partial class IksungReader
{
    /// <summary>NTag 칩 버전 정보를 읽는다 (칩 종류, 제조일 등).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>8바이트 버전 데이터 (Storage Size, Product Type 등 포함)</returns>
    public async Task<byte[]> NTagGetVersionAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_GET_VERSION,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// NTag Fast Read — 연속된 페이지를 한 번에 읽는다.
    /// </summary>
    /// <param name="startPage">시작 페이지 번호</param>
    /// <param name="endPage">끝 페이지 번호 (포함)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>페이지 데이터 연속 배열 (각 페이지 4바이트)</returns>
    public async Task<byte[]> NTagFastReadAsync(byte startPage, byte endPage,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_FAST_READ,
            data: [startPage, endPage], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag 카운터 값을 읽는다.</summary>
    /// <param name="counterNo">카운터 번호 (NTag 216: 0–2)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>3바이트 little-endian 카운터 값</returns>
    public async Task<byte[]> NTagReadCounterAsync(byte counterNo = 0,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_COUNTER_READ,
            data: [counterNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag ECC 서명을 읽는다 (칩 진위 확인용).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>32바이트 ECC 서명</returns>
    public async Task<byte[]> NTagReadSignatureAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_READ_SIGN_ECC,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag AUTH0 설정을 쓴다 (패스워드 보호 시작 페이지).</summary>
    /// <param name="auth0">패스워드 보호가 시작되는 페이지 번호 (0xFF = 보호 없음)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task NTagWriteAuth0Async(byte auth0, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_AUTH0_WRITE,
            data: [auth0], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag ACCESS 설정을 쓴다.</summary>
    /// <param name="access">ACCESS 바이트 (bit2=PROT: 0=쓰기만, 1=읽기/쓰기 보호)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task NTagWriteAccessAsync(byte access, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_ACCESS_WRITE,
            data: [access], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>NTag 패스워드를 변경한다.</summary>
    /// <param name="newPassword">새 4바이트 패스워드</param>
    /// <param name="pack">새 2바이트 PACK (Password ACKnowledge)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task NTagChangePasswordAsync(byte[] newPassword, byte[] pack,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (newPassword.Length != 4) throw new ArgumentException("NTag 패스워드는 4바이트여야 합니다.", nameof(newPassword));
        if (pack.Length != 2)        throw new ArgumentException("PACK은 2바이트여야 합니다.", nameof(pack));
        byte[] data = new byte[6];
        newPassword.CopyTo(data, 0);
        pack.CopyTo(data, 4);
        await SendCommandAsync(Constants.MAJOR_MIFARE_ULTRALIGHT, Constants.NTAG_PASSWORD_CHANGE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// NTag 칩 타입을 버전 데이터에서 판별한다.
    /// </summary>
    /// <param name="versionData"><see cref="NTagGetVersionAsync"/>의 반환값</param>
    /// <returns>"NTag213", "NTag215", "NTag216", 또는 "Unknown"</returns>
    public static string ParseNTagType(byte[] versionData)
    {
        if (versionData.Length < 8) return "Unknown";
        // Storage size: byte[6]
        return versionData[6] switch
        {
            0x0F => "NTag213",   // 144 bytes user memory
            0x11 => "NTag215",   // 504 bytes user memory
            0x13 => "NTag216",   // 888 bytes user memory
            _    => $"Unknown (storage=0x{versionData[6]:X2})"
        };
    }
}
