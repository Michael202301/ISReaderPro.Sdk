using Iksung.Reader.Internals.Channels;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>MAJOR_AUTO (0x0A) 명령군 — AutoRead 폴링 시작/중지 및 패킷 파싱.</summary>
public sealed partial class IksungReader
{
    // ─── AutoRead 모드 ────────────────────────────────────────

    /// <summary>
    /// AutoRead 폴링을 시작한다.
    /// 카드가 감지될 때마다 <see cref="TagDetected"/> 이벤트가 발생한다.
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task StartAutoReadAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_AUTO, Constants.AUTOSETUP_POLLING_START,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        _autoReadActive = true;
    }

    /// <summary>AutoRead 폴링을 중지한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task StopAutoReadAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        _autoReadActive = false;
        await SendCommandAsync(
            Constants.MAJOR_AUTO, Constants.AUTOSETUP_POLLING_STOP,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Private helpers ─────────────────────────────────────

    private static (CardType, byte[]) MapAutoReadPacket(IksungPacket pkt)
    {
        byte cmd2 = (byte)(pkt.Cmd2 & ~Constants.BUZZER_FLAG);
        return cmd2 switch
        {
            Constants.AUTOSETUP_ISO14443A_UID         => (CardType.Iso14443a,        pkt.Data),
            Constants.AUTOSETUP_ISO14443B_UID         => (CardType.Iso14443b,        pkt.Data),
            Constants.AUTOSETUP_ISO14443A_MIFARE1 or
            Constants.AUTOSETUP_ISO14443A_MIFARE2 or
            Constants.AUTOSETUP_ISO14443A_MIFARE3 or
            Constants.AUTOSETUP_ISO14443A_MIFARE4     => (CardType.MifareClassic,    pkt.Data),
            Constants.AUTOSETUP_MIFARE_UL or
            Constants.AUTOSETUP_MIFARE_NTAG           => (CardType.MifareUltralight, pkt.Data),
            Constants.AUTOSETUP_ISO15693_UID          => (CardType.Iso15693,         pkt.Data),
            Constants.AUTOSETUP_FELICA_UID            => (CardType.Felica,           pkt.Data),
            Constants.AUTOSETUP_LF_EM_UID or
            Constants.AUTOSETUP_LF_T5577_LONGDATA_UID => (CardType.Lf125Khz,        pkt.Data),
            _                                         => (CardType.Unknown,          pkt.Data),
        };
    }
}
