using System.Text;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_COMMON (0x00) 명령군 — 버전, UID, RF On/Off, 부저, 교통카드 일련번호 등 공통 기능.</summary>
public sealed partial class IksungReader
{
    // ─── 장치 정보 ────────────────────────────────────────────

    /// <summary>
    /// 펌웨어 버전 문자열을 읽는다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>버전 문자열 (예: "IS-3500K_V1.8")</returns>
    public async Task<string> ReadVersionAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] resp = await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_VERSION,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return Encoding.ASCII.GetString(resp).TrimEnd('\0').Trim();
    }

    /// <summary>장치 고유 ID (UID)를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadUniqueIdAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_UNIQUE_ID,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── RF 제어 ─────────────────────────────────────────────

    /// <summary>RF 안테나를 켠다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task RfOnAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_COMMON, Constants.COMMON_RFON,
                            timeoutMs: timeoutMs, ct: ct);

    /// <summary>RF 안테나를 끈다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public Task RfOffAsync(int timeoutMs = 1000, CancellationToken ct = default)
        => SendCommandAsync(Constants.MAJOR_COMMON, Constants.COMMON_RFOFF,
                            timeoutMs: timeoutMs, ct: ct);

    // ─── 부저 ────────────────────────────────────────────────

    /// <summary>
    /// 내장 부저를 울린다.
    /// </summary>
    /// <param name="durationUnit">울림 시간 단위 (1 = 100ms, 2 = 200ms, …). 기본값 2 = 200ms.</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BuzzerAsync(byte durationUnit = 2, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_BUZZER,
            data: [durationUnit], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 카드 타입 / SAK / ATS ───────────────────────────────

    /// <summary>
    /// 현재 필드에 있는 ISO 14443-A 카드의 SAK (Select Acknowledge) 타입 바이트를 읽는다.
    /// 카드가 이미 활성화된 상태에서 호출한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadSakTypeAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_SAK_TYPE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// ISO 14443-4A 카드의 ATS (Answer To Select) 데이터를 읽는다.
    /// 카드가 이미 활성화된 상태에서 호출한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadAtsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ATS,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 현재 필드에 있는 태그의 종류를 감지한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>태그 타입 코드 바이트 배열</returns>
    public async Task<byte[]> ReadTagTypeAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_TAG_TYPE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// ISO 14443-A/B, FeliCa, ISO 15693 카드를 자동 감지하여 UID를 반환한다.
    /// 카드 종류에 무관하게 단일 호출로 UID를 읽을 수 있다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>UID 바이트 배열</returns>
    public async Task<byte[]> ReadAllUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ALL_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 전체 카드 종류를 읽는다 (ALL_CARD_TYPE).
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadAllCardTypeAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ALL_CARD_TYPE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── UID 단순 읽기 (MAJOR_COMMON 군) ─────────────────────

    /// <summary>
    /// ISO 14443-A 카드 UID를 읽는다.
    /// CMD1=MAJOR_COMMON(0x00), CMD2=COMMON_ISO14443A_UID_READ — Common 명령군에 속함.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadIso14443aUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO14443A_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// ISO 14443-B 카드 UID를 읽는다.
    /// CMD1=MAJOR_COMMON(0x00), CMD2=COMMON_ISO14443B_UID_READ — Common 명령군에 속함.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadIso14443bUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO14443B_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// FeliCa 카드 UID를 읽는다.
    /// CMD1=MAJOR_COMMON(0x00), CMD2=COMMON_FELICA_UID_READ — Common 명령군에 속함.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadFelicaUidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_FELICA_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// ISO 15693 카드 UID를 읽는다.
    /// CMD1=MAJOR_COMMON(0x00), CMD2=COMMON_ISO15693_UID_READ — Common 명령군에 속함.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadIso15693UidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ISO15693_UID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 교통카드 일련번호 ────────────────────────────────────

    /// <summary>T-Money 교통카드 일련번호를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadTmoneySerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_TMONEY_SERIAL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>CashBee 교통카드 일련번호를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadCashbeeSerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_CASHBEE_SERIAL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>K-Cash 교통카드 일련번호를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadKcashSerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_KCASH_SERIAL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>전체 교통카드(T-Money / CashBee / K-Cash / RailPlus) 일련번호를 자동 감지하여 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadAllCashSerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_ALL_CASH_SERIAL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>RailPlus 교통카드 일련번호를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadRailPlusSerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_RAILPLUS_SERIAL,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── WDT (Watchdog Timer) ────────────────────────────────

    /// <summary>Watchdog 타임아웃 설정값을 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> ReadWdtTimeoutAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_WDT_TIMEOUT_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Watchdog 타임아웃 설정값을 쓴다.</summary>
    /// <param name="timeoutValue">타임아웃 값 (펌웨어 정의 단위)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task WriteWdtTimeoutAsync(byte timeoutValue, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_COMMON, Constants.COMMON_WDT_TIMEOUT_WRITE,
            data: [timeoutValue], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
