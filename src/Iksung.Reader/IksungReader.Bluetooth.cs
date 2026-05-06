using System.Text;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>Bluetooth / BLE 설정 관련 명령 (MAJOR_BLE_CONFIG = 0x12).</summary>
public sealed partial class IksungReader
{
    // ─── BLE 기본 설정 ────────────────────────────────────────

    /// <summary>BLE 장치 이름을 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>BLE 장치 이름 문자열</returns>
    public async Task<string> BleReadNameAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] resp = await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_NAME_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return Encoding.ASCII.GetString(resp).TrimEnd('\0').Trim();
    }

    /// <summary>BLE 장치 이름을 변경하고 Flash에 저장한다 (재시작 후 적용).</summary>
    /// <param name="name">새 BLE 장치 이름 (최대 20자)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteNameAsync(string name, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = Encoding.ASCII.GetBytes(name);
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_NAME_WRITE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE MAC 주소를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>6바이트 MAC 주소 (little-endian)</returns>
    public async Task<byte[]> BleReadMacAddressAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_MAC_ADDRESS_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE TX 전송 파워를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>TX 파워 인덱스 바이트 (0=-40dBm, 7=4dBm, 8=8dBm)</returns>
    public async Task<byte[]> BleReadTxPowerAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_TX_POWER_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE TX 전송 파워를 설정한다.</summary>
    /// <param name="powerIndex">TX 파워 인덱스 (0=-40dBm, 5=0dBm, 7=4dBm, 8=8dBm)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteTxPowerAsync(byte powerIndex, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_TX_POWER_WRITE,
            data: [powerIndex], timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE GAP 연결 파라미터를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> BleReadGapConnectParamsAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_GAP_CONNECT_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE 모듈을 재시작(시스템 리셋)한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleSystemResetAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_SYSTEM_RESET,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── BLE Central 설정 ────────────────────────────────────

    /// <summary>BLE Central 모드 활성화 상태를 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> BleReadCentralEnableAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_ENABLE_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE Central 스캔을 시작한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleCentralScanStartAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_SCAN_START,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE Central 스캔을 중지한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleCentralScanStopAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_SCAN_STOP,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE Central 스캔 결과 목록을 읽는다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<byte[]> BleCentralScanListAsync(int timeoutMs = 2000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_SCAN_LIST,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── BLE Peripheral 설정 ─────────────────────────────────

    /// <summary>BLE Peripheral 광고를 시작한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BlePeripheralAdvertisingStartAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_ADVERTSING_START,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE Peripheral 광고를 중지한다.</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BlePeripheralAdvertisingStopAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_ADVERTSING_STOP,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 유틸리티 ────────────────────────────────────────────

    /// <summary>TX 파워 인덱스를 dBm 문자열로 변환한다.</summary>
    /// <param name="powerIndex">TX 파워 인덱스 바이트</param>
    public static string TxPowerToString(byte powerIndex) => powerIndex switch
    {
        0 => "-40 dBm",
        1 => "-20 dBm",
        2 => "-16 dBm",
        3 => "-12 dBm",
        4 => "-8 dBm",
        5 => "0 dBm",
        6 => "4 dBm",  // some firmware variants
        7 => "4 dBm",
        8 => "8 dBm",
        _ => $"? ({powerIndex})"
    };
}
