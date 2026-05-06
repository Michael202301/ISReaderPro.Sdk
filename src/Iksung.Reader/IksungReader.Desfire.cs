using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>DESFire EV1/EV2/EV3 관련 명령 (MAJOR_DESFIRE = 0x09).</summary>
public sealed partial class IksungReader
{
    // ─── DESFire 활성화 / 정보 ─────────────────────────────────

    /// <summary>DESFire 카드를 활성화하고 UID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ActivateDesfireAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_DESFIRE, Constants.DESFIRE_ACTIVE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>DESFire 카드의 남은 메모리 바이트 수를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>3바이트 little-endian 여유 메모리 크기</returns>
    public async Task<byte[]> DesfireGetFreeMemoryAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_DESFIRE, Constants.DESFIRE_FREE_MEMORY,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>DESFire 카드 UID를 읽는다 (인증 후 사용 가능).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> DesfireGetUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_DESFIRE, Constants.DESFIRE_GET_UID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>DESFire 카드를 포맷한다 (Master Key 인증 후 실행).</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireFormatAsync(int timeoutMs = 3000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_DESFIRE, Constants.DESFIRE_FORMAT,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire 키 관리 ──────────────────────────────────────

    /// <summary>
    /// DESFire 인증 키를 리더기 내부 Flash에 저장한다.
    /// 인증 명령 전에 반드시 먼저 호출해야 한다.
    /// </summary>
    /// <param name="keyIndex">리더기 내부 키 인덱스 (0–15)</param>
    /// <param name="keyVersion">키 버전 바이트</param>
    /// <param name="cryptoType">암호화 타입 (0x00=AES-128, 0x04=2K3DES, 0x05=3K3DES)</param>
    /// <param name="key">키 데이터 (AES: 16바이트, 2K3DES: 16바이트, 3K3DES: 24바이트)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireKeySaveAsync(byte keyIndex, byte keyVersion, byte cryptoType, byte[] key,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = new byte[3 + key.Length];
        data[0] = keyIndex;
        data[1] = keyVersion;
        data[2] = cryptoType;
        key.CopyTo(data, 3);
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_KEYSAVE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>DESFire 내부 저장 키를 초기화(기본값으로 리셋)한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireKeyInitAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_KEY_INIT,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire 인증 ─────────────────────────────────────────

    /// <summary>DESFire AES 인증. 사전에 <see cref="DesfireKeySaveAsync"/>로 키를 저장해야 한다.</summary>
    /// <param name="keyNo">카드의 키 번호 (0=Master Key)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireAuthenticateAesAsync(byte keyNo = 0,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_AUTH_AES,
            data: [keyNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>DESFire 2K3DES 인증.</summary>
    /// <param name="keyNo">카드의 키 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireAuthenticate2K3DesAsync(byte keyNo = 0,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_AUTH_2K3DES,
            data: [keyNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>DESFire ISO 인증 (ISO/IEC 7816-4 방식).</summary>
    /// <param name="keyNo">카드의 키 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireAuthenticateIsoAsync(byte keyNo = 0,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_AUTH_ISO,
            data: [keyNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>인증 상태를 초기화(Reset)한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireAuthResetAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_AUTH_RESET,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire Application 관리 ─────────────────────────────

    /// <summary>카드에 존재하는 모든 Application ID를 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>각 3바이트씩, N개 App ID</returns>
    public async Task<byte[]> DesfireGetApplicationIdsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_GET_APPIDS,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Application을 선택한다.</summary>
    /// <param name="appId">3바이트 Application ID (예: new byte[]{0x01,0x02,0x03})</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireSelectApplicationAsync(byte[] appId,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (appId.Length != 3) throw new ArgumentException("DESFire App ID는 3바이트여야 합니다.", nameof(appId));
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_SELECT_APPIDS,
            data: appId, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Root Application (ID = 00 00 00)을 선택한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task DesfireSelectRootAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => DesfireSelectApplicationAsync([0x00, 0x00, 0x00], timeoutMs, ct);

    /// <summary>Application을 삭제한다 (Master Key 인증 후 실행).</summary>
    /// <param name="appId">3바이트 Application ID</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireDeleteApplicationAsync(byte[] appId,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (appId.Length != 3) throw new ArgumentException("DESFire App ID는 3바이트여야 합니다.", nameof(appId));
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_DELETE_APPIDS,
            data: appId, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 새 Application을 생성한다.
    /// </summary>
    /// <param name="appId">3바이트 Application ID</param>
    /// <param name="keySettings">키 접근 권한 (1바이트)</param>
    /// <param name="keyCount">Application이 가질 키 수 (1–14)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireCreateApplicationAsync(byte[] appId, byte keySettings, byte keyCount,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (appId.Length != 3) throw new ArgumentException("DESFire App ID는 3바이트여야 합니다.", nameof(appId));
        byte[] data = new byte[7];
        appId.CopyTo(data, 0);
        data[3] = keySettings;
        data[4] = keyCount;
        data[5] = 0x00; // ISO File ID (없음)
        data[6] = 0x00;
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_CREATE_APPIDS,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire File 관리 ────────────────────────────────────

    /// <summary>선택된 Application의 File ID 목록을 반환한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> DesfireGetFileIdsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_GET_FILE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>파일 설정 정보를 조회한다.</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> DesfireGetFileSettingsAsync(byte fileNo,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_GET_FILE_SETTINGS,
            data: [fileNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Standard Data File을 생성한다.
    /// </summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="commSettings">통신 설정 (0=Plain, 1=MAC, 3=Encrypt)</param>
    /// <param name="accessRights">2바이트 접근 권한</param>
    /// <param name="fileSize">파일 크기 (바이트)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireCreateStdFileAsync(byte fileNo, byte commSettings,
        ushort accessRights, int fileSize,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = new byte[8];
        data[0] = fileNo;
        data[1] = 0x00; // ISO File ID 없음
        data[2] = 0x00;
        data[3] = commSettings;
        data[4] = (byte)(accessRights & 0xFF);
        data[5] = (byte)(accessRights >> 8);
        data[6] = (byte)(fileSize & 0xFF);
        data[7] = (byte)((fileSize >> 8) & 0xFF);
        // 3바이트 LE이므로 3번째 바이트도 포함
        byte[] data2 = new byte[9];
        data.CopyTo(data2, 0);
        data2[8] = (byte)((fileSize >> 16) & 0xFF);
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_CREATE_STD_FILE,
            data: data2, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>파일을 삭제한다.</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireDeleteFileAsync(byte fileNo,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_DELETE_FILE,
            data: [fileNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire 데이터 읽기/쓰기 ────────────────────────────

    /// <summary>Standard Data File / Backup File에서 데이터를 읽는다.</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="commSettings">통신 설정 (0=Plain, 1=MAC, 3=Encrypt)</param>
    /// <param name="offset">읽기 시작 오프셋 (바이트)</param>
    /// <param name="length">읽을 바이트 수 (0=전체)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> DesfireReadDataFileAsync(byte fileNo, byte commSettings = 0,
        int offset = 0, int length = 0,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = new byte[8];
        data[0] = commSettings;
        data[1] = fileNo;
        data[2] = (byte)(offset & 0xFF);
        data[3] = (byte)((offset >> 8) & 0xFF);
        data[4] = (byte)((offset >> 16) & 0xFF);
        data[5] = (byte)(length & 0xFF);
        data[6] = (byte)((length >> 8) & 0xFF);
        data[7] = (byte)((length >> 16) & 0xFF);
        return await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_READ_DATA_FILE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Standard Data File / Backup File에 데이터를 쓴다.</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="payload">쓸 데이터</param>
    /// <param name="commSettings">통신 설정 (0=Plain, 1=MAC, 3=Encrypt)</param>
    /// <param name="offset">쓰기 시작 오프셋 (바이트)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireWriteDataFileAsync(byte fileNo, byte[] payload, byte commSettings = 0,
        int offset = 0,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        int length = payload.Length;
        byte[] data = new byte[8 + length];
        data[0] = commSettings;
        data[1] = fileNo;
        data[2] = (byte)(offset & 0xFF);
        data[3] = (byte)((offset >> 8) & 0xFF);
        data[4] = (byte)((offset >> 16) & 0xFF);
        data[5] = (byte)(length & 0xFF);
        data[6] = (byte)((length >> 8) & 0xFF);
        data[7] = (byte)((length >> 16) & 0xFF);
        payload.CopyTo(data, 8);
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_WRITE_DATA_FILE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire Value File ───────────────────────────────────

    /// <summary>Value File의 값을 읽는다.</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="commSettings">통신 설정</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>4바이트 little-endian 정수</returns>
    public async Task<byte[]> DesfireReadValueFileAsync(byte fileNo, byte commSettings = 0,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_READ_VALUE_FILE,
            data: [commSettings, fileNo], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Value File에 값을 증가시킨다 (Credit).</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="amount">증가량 (4바이트 부호 있는 정수)</param>
    /// <param name="commSettings">통신 설정</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireCreditValueFileAsync(byte fileNo, int amount, byte commSettings = 0,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = new byte[6];
        data[0] = commSettings;
        data[1] = fileNo;
        data[2] = (byte)(amount & 0xFF);
        data[3] = (byte)((amount >> 8) & 0xFF);
        data[4] = (byte)((amount >> 16) & 0xFF);
        data[5] = (byte)((amount >> 24) & 0xFF);
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_WRITE_VALUE_FILE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Value File에서 값을 감소시킨다 (Debit).</summary>
    /// <param name="fileNo">파일 번호</param>
    /// <param name="amount">감소량</param>
    /// <param name="commSettings">통신 설정</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireDebitValueFileAsync(byte fileNo, int amount, byte commSettings = 0,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = new byte[6];
        data[0] = commSettings;
        data[1] = fileNo;
        data[2] = (byte)(amount & 0xFF);
        data[3] = (byte)((amount >> 8) & 0xFF);
        data[4] = (byte)((amount >> 16) & 0xFF);
        data[5] = (byte)((amount >> 24) & 0xFF);
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_DEBIT_VALUE_FILE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── DESFire Transaction ─────────────────────────────────

    /// <summary>진행 중인 Transaction을 커밋(확정)한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireCommitTransactionAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_COMMIT_TRANSACTION,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>진행 중인 Transaction을 중단(롤백)한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task DesfireAbortTransactionAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_DESFIRE, Constants.DESFIRE_ABORT_TRANSACTION,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
