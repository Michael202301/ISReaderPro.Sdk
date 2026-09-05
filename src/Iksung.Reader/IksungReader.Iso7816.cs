using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>ISO 7816 (USIM / Smart Card) 관련 명령 (MAJOR_ISO7816 = 0x0A).</summary>
public sealed partial class IksungReader
{
    /// <summary>
    /// ISO 7816 스마트카드(USIM)를 활성화한다.
    /// </summary>
    /// <param name="channel">카드 슬롯/채널 번호 (기본값: 0)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>ATR (Answer To Reset) 바이트 배열</returns>
    public async Task<byte[]> UsimActivateAsync(byte channel = 0,
        int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO7816, Constants.USIM_ACTIVE,
            data: [channel], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>ISO 7816 스마트카드(USIM)를 비활성화한다.</summary>
    /// <param name="channel">카드 슬롯/채널 번호 (기본값: 0)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task UsimDeactivateAsync(byte channel = 0,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_ISO7816, Constants.USIM_DEACTIVE,
            data: [channel], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// ISO 7816 T=0 또는 T=1 TPDU 명령을 전송하고 응답을 받는다.
    /// </summary>
    /// <param name="tpdu">TPDU 바이트 배열 (CLA INS P1 P2 [Lc Data] [Le])</param>
    /// <param name="channel">카드 슬롯/채널 번호 (기본값: 0)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>응답 TPDU (Data + SW1 SW2)</returns>
    public async Task<byte[]> UsimSendTpduAsync(byte[] tpdu, byte channel = 0,
        int timeoutMs = 3000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = new byte[1 + tpdu.Length];
        data[0] = channel;
        tpdu.CopyTo(data, 1);
        return await SendCommandAsync(
            Constants.MAJOR_ISO7816, Constants.USIM_TPDU_COMMAND,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>USIM 직렬 번호(CCID)를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> UsimReadSerialAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_ISO7816, Constants.USIM_CNCRF_SERIAL_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }
}
