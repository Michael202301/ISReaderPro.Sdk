using System.Text;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader;

/// <summary>Bluetooth / BLE 설정 관련 명령 (MAJOR_BLE_CONFIG = 0x12).</summary>
public sealed partial class IksungReader
{
    // ─── BLE 기본 설정 ────────────────────────────────────────

    /// <summary>BLE 장치 이름을 읽는다. (<c>0x10</c>, 응답 <c>[enable(1)][name(N)]</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>BLE 장치 이름 문자열 (UTF-8). 선행 enable 바이트는 제외하고 반환한다.</returns>
    public async Task<string> BleReadNameAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] resp = await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_NAME_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        // 응답: [enable(1)][name(N)] — 첫 enable 바이트를 제외한 나머지가 이름(UTF-8).
        if (resp.Length <= 1) return string.Empty;
        return Encoding.UTF8.GetString(resp, 1, resp.Length - 1).TrimEnd('\0').Trim();
    }

    /// <summary>BLE 장치 이름을 변경하고 Flash에 저장한다 (재시작 후 적용). 이름 사용은 활성(enable=1). (<c>0x11</c>)</summary>
    /// <param name="name">새 BLE 장치 이름. BLE 표준상 최대 28바이트(UTF-8)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="name"/> 의 UTF-8 길이가 28바이트를 초과</exception>
    public Task BleWriteNameAsync(string name, int timeoutMs = 1000, CancellationToken ct = default)
        => BleWriteNameAsync(name, enable: true, timeoutMs: timeoutMs, ct: ct);

    /// <summary>BLE 장치 이름과 사용 여부(enable)를 변경하고 Flash에 저장한다 (재시작 후 적용). (<c>0x11</c>, <c>[enable(1)][name(N)]</c>)</summary>
    /// <param name="name">새 BLE 장치 이름. BLE 표준상 최대 28바이트(UTF-8)</param>
    /// <param name="enable">Advertising 시 장치 이름 사용 여부</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="name"/> 의 UTF-8 길이가 28바이트를 초과</exception>
    public async Task BleWriteNameAsync(string name, bool enable, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] nameBytes = Encoding.UTF8.GetBytes(name ?? string.Empty);
        if (nameBytes.Length > 28)
            throw new ArgumentOutOfRangeException(nameof(name), "BLE 장치 이름은 UTF-8 기준 최대 28바이트입니다.");
        // 요청: [enable(1)][name(N)]
        byte[] data = new byte[1 + nameBytes.Length];
        data[0] = (byte)(enable ? 1 : 0);
        Array.Copy(nameBytes, 0, data, 1, nameBytes.Length);
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

    /// <summary>
    /// BLE RF 전송 파워를 읽는다. (<c>0x14</c>, 응답 <c>[centralIdx][peripheralIdx]</c> 2-byte)
    /// <para>인덱스→dBm 매핑은 <see cref="TxPowerToString"/> 참조 (0=-40 … 5=-4 … 6=0 … 7=4(기본) … 8=8 dBm).</para>
    /// <para>Central/Peripheral 을 구분해 받으려면 <see cref="BleReadRfPowerAsync"/> 를 사용할 것.</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>RF 파워 인덱스 바이트 배열. <c>[0]</c>=Central, <c>[1]</c>=Peripheral</returns>
    public async Task<byte[]> BleReadTxPowerAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_TX_POWER_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// BLE RF 전송 파워를 Central·Peripheral 동일 인덱스로 설정한다.
    /// (<c>0x15</c>, <c>[centralIdx][peripheralIdx]</c> — 동일 값으로 둘 다 적용)
    /// <para>Central/Peripheral 을 개별 설정하려면 <see cref="BleWriteRfPowerAsync"/> 를 사용할 것.</para>
    /// </summary>
    /// <param name="powerIndex">RF 파워 인덱스 (0=-40dBm … 5=-4dBm, 6=0dBm, 7=4dBm(기본), 8=8dBm)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteTxPowerAsync(byte powerIndex, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        // 0x15 는 [centralIdx][peripheralIdx] 2-byte — 단일 인덱스 호출은 양쪽 동일 적용.
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_TX_POWER_WRITE,
            data: new[] { powerIndex, powerIndex }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// BLE RF 전송 파워(Central/Peripheral)를 읽는다. (<c>0x14</c>, 응답 <c>[centralIdx][peripheralIdx]</c>)
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>(Central, Peripheral) RF 파워 인덱스. 1-byte 구형 응답은 동일 값으로 채워 하위호환 처리.</returns>
    public async Task<(byte Central, byte Peripheral)> BleReadRfPowerAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_TX_POWER_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        byte central    = data.Length >= 1 ? data[0] : (byte)0;
        byte peripheral  = data.Length >= 2 ? data[1] : central;   // 1-byte(구형) 하위호환
        return (central, peripheral);
    }

    /// <summary>
    /// BLE RF 전송 파워를 Central·Peripheral 개별 인덱스로 설정한다.
    /// (<c>0x15</c>, <c>[centralIdx][peripheralIdx]</c> 2-byte)
    /// </summary>
    /// <param name="centralIndex">Central RF 파워 인덱스 (0~8)</param>
    /// <param name="peripheralIndex">Peripheral RF 파워 인덱스 (0~8)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentOutOfRangeException">인덱스가 0~8 범위를 벗어남</exception>
    public async Task BleWriteRfPowerAsync(byte centralIndex, byte peripheralIndex, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (centralIndex > 8)
            throw new ArgumentOutOfRangeException(nameof(centralIndex), "0~8");
        if (peripheralIndex > 8)
            throw new ArgumentOutOfRangeException(nameof(peripheralIndex), "0~8");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_TX_POWER_WRITE,
            data: new[] { centralIndex, peripheralIndex }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
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

    // ─── 연결 상태 진단 (V1.37 §12) ─────────────────────────

    /// <summary>
    /// BLE Central 연결 상태를 읽는다. (펌웨어 V1.37 §12)
    /// <para>2-byte <c>[state][tx_ready]</c> 응답을 우선 파싱하고, 1-byte(V1.36) 응답은 하위호환 처리한다.</para>
    /// <para>데이터 전송 가능 여부는 <see cref="BleConnectState.TxReady"/> 로 판단할 것.</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<BleConnectState> BleReadCentralConnectStateAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_CONNECT_STATE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return ParseConnectState(data, centralLegacy: true);
    }

    /// <summary>
    /// BLE Peripheral 연결 상태를 읽는다. (펌웨어 V1.37 §12)
    /// <para>2-byte <c>[state][tx_ready]</c> 응답을 우선 파싱하고, 1-byte(V1.36) 응답은 하위호환 처리한다.</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<BleConnectState> BleReadPeripheralConnectStateAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(
            Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_CONNECT_STATE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return ParseConnectState(data, centralLegacy: false);
    }

    // 2-byte 우선 / 1-byte(V1.36) 하위호환 파싱.
    // V1.36 1-byte: Central state==2 = connect, Peripheral state==1 = connect.
    private static BleConnectState ParseConnectState(byte[] data, bool centralLegacy)
    {
        if (data.Length >= 2)
            return new BleConnectState(data[0], data[1] == 1);

        byte s = data.Length >= 1 ? data[0] : (byte)0;
        bool connected = centralLegacy ? (s == 2) : (s == 1);
        return new BleConnectState(connected ? (byte)0x03 : (byte)0x00, connected);
    }

    // ─── 시스템 리셋 scope (V1.37 §11) ───────────────────────

    /// <summary>
    /// BLE 모듈을 지정한 범위로 재시작한다. (펌웨어 V1.37 §11)
    /// <para>무인자 <see cref="BleSystemResetAsync(int, CancellationToken)"/> 는 Full reset 하위호환.</para>
    /// <para><see cref="BleResetScope.Full"/> 은 응답 직후 MCU 재부팅하므로 호스트는 재연결 대기가 필요하다.</para>
    /// </summary>
    /// <param name="scope">재시작 범위</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleSystemResetAsync(BleResetScope scope, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_SYSTEM_RESET,
            data: new[] { (byte)scope }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Boot Health 진단 (V1.40 §13) ───────────────────────

    /// <summary>
    /// 부팅 진단(Boot Health) 결과를 읽는다. (펌웨어 V1.40 §13)
    /// <para>24-byte 응답 중 앞 16-byte 가 핵심 subsystem init 결과(int8, 0=OK).</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task<BleBootHealth> BleReadBootHealthAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_BOOT_HEALTH_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);

        int n = Math.Min(16, data.Length);
        var results = new sbyte[n];
        for (int i = 0; i < n; i++) results[i] = (sbyte)data[i];
        return new BleBootHealth(results);
    }

    // ─── GAP 연결 파라미터 Write + 검증 (V1.37 §10) ──────────

    /// <summary>
    /// BLE GAP 연결 파라미터를 설정한다. (펌웨어 V1.37 §10, <c>0x13</c>)
    /// <para>모든 값은 16-bit LE 로 전송. 범위 위반 시 송신 전 <see cref="ArgumentOutOfRangeException"/> 을 던지고,
    /// 펌웨어가 거부하면 <see cref="Exceptions.IksungProtocolException"/> (state=0xFF) 가 발생한다.</para>
    /// </summary>
    /// <param name="minConnInterval">최소 연결 간격 (6~3200, ×1.25ms)</param>
    /// <param name="maxConnInterval">최대 연결 간격 (6~3200, ×1.25ms, min≤max)</param>
    /// <param name="slaveLatency">slave latency (0~499)</param>
    /// <param name="connSupTimeout">supervision timeout (10~3200, ×10ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteGapConnectParamsAsync(
        int minConnInterval, int maxConnInterval, int slaveLatency, int connSupTimeout,
        int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (minConnInterval < 6 || minConnInterval > 3200)
            throw new ArgumentOutOfRangeException(nameof(minConnInterval), "6~3200 (×1.25ms)");
        if (maxConnInterval < 6 || maxConnInterval > 3200)
            throw new ArgumentOutOfRangeException(nameof(maxConnInterval), "6~3200 (×1.25ms)");
        if (minConnInterval > maxConnInterval)
            throw new ArgumentOutOfRangeException(nameof(minConnInterval), "minConnInterval ≤ maxConnInterval 이어야 함");
        if (slaveLatency < 0 || slaveLatency > 499)
            throw new ArgumentOutOfRangeException(nameof(slaveLatency), "0~499");
        if (connSupTimeout < 10 || connSupTimeout > 3200)
            throw new ArgumentOutOfRangeException(nameof(connSupTimeout), "10~3200 (×10ms)");
        // supervision margin: timeout×4 > (1+latency)×max
        if ((long)connSupTimeout * 4 <= (long)(1 + slaveLatency) * maxConnInterval)
            throw new ArgumentOutOfRangeException(nameof(connSupTimeout),
                "supervision margin 위반: connSupTimeout×4 > (1+slaveLatency)×maxConnInterval 이어야 함");

        byte[] data = new byte[8];
        WriteU16Le(data, 0, minConnInterval);
        WriteU16Le(data, 2, maxConnInterval);
        WriteU16Le(data, 4, slaveLatency);
        WriteU16Le(data, 6, connSupTimeout);
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_GAP_CONNECT_WRITE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Advertising Interval R/W (V1.37 §10) ────────────────

    /// <summary>BLE 광고 간격(ms)을 읽는다. (<c>0x3A</c>, 2-byte BE)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>광고 간격 (ms)</returns>
    public async Task<int> BleReadAdvIntervalAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_PERIPHERAL_ADV_INTERVAL_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return ReadU16Be(data, 0);
    }

    /// <summary>BLE 광고 간격을 설정한다. (<c>0x3B</c>, 2-byte BE, 20~10240ms)</summary>
    /// <param name="intervalMs">광고 간격 (20~10240 ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteAdvIntervalAsync(int intervalMs, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (intervalMs < 20 || intervalMs > 10240)
            throw new ArgumentOutOfRangeException(nameof(intervalMs), "20~10240 ms");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_ADV_INTERVAL_SAVE,
            data: U16Be(intervalMs), timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Received Timeout R/W (V1.37 §10) ────────────────────

    /// <summary>BLE 패킷 수신 타임아웃(ms)을 읽는다. (<c>0x52</c>, 2-byte BE)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>수신 타임아웃 (ms)</returns>
    public async Task<int> BleReadReceivedTimeoutAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_RECIVED_TIMEOUT_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return ReadU16Be(data, 0);
    }

    /// <summary>BLE 패킷 수신 타임아웃을 설정한다. (<c>0x53</c>, 2-byte BE, 1~10000ms)</summary>
    /// <param name="recvTimeoutMs">수신 타임아웃 (1~10000 ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteReceivedTimeoutAsync(int recvTimeoutMs, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (recvTimeoutMs < 1 || recvTimeoutMs > 10000)
            throw new ArgumentOutOfRangeException(nameof(recvTimeoutMs), "1~10000 ms");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_RECIVED_TIMEOUT_SAVE,
            data: U16Be(recvTimeoutMs), timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── No-Protocol Mode R/W (V1.37 §10) ────────────────────

    /// <summary>Peripheral No-Protocol(raw passthrough) 모드 설정을 읽는다. (<c>0x55</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns><c>Enable</c>=활성 여부, <c>TimeoutMs</c>=무프로토콜 타임아웃(ms)</returns>
    public async Task<(bool Enable, int TimeoutMs)> BleReadPeripheralNoProtocolAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_PERIPHERAL_NO_PROTOCOL_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return ParseNoProtocol(data);
    }

    /// <summary>Peripheral No-Protocol(raw passthrough) 모드를 설정한다. (<c>0x56</c>, timeout 2-byte BE, 1~10000ms)</summary>
    /// <param name="enable">활성 여부</param>
    /// <param name="noProtocolTimeoutMs">무프로토콜 타임아웃 (1~10000 ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWritePeripheralNoProtocolAsync(bool enable, int noProtocolTimeoutMs, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (noProtocolTimeoutMs < 1 || noProtocolTimeoutMs > 10000)
            throw new ArgumentOutOfRangeException(nameof(noProtocolTimeoutMs), "1~10000 ms");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_NO_PROTOCOL_SAVE,
            data: BuildNoProtocol(enable, noProtocolTimeoutMs), timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Central No-Protocol(raw passthrough) 모드 설정을 읽는다. (<c>0x57</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns><c>Enable</c>=활성 여부, <c>TimeoutMs</c>=무프로토콜 타임아웃(ms)</returns>
    public async Task<(bool Enable, int TimeoutMs)> BleReadCentralNoProtocolAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_CENTRAL_NO_PROTOCOL_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return ParseNoProtocol(data);
    }

    /// <summary>Central No-Protocol(raw passthrough) 모드를 설정한다. (<c>0x58</c>, timeout 2-byte BE, 1~10000ms)</summary>
    /// <param name="enable">활성 여부</param>
    /// <param name="noProtocolTimeoutMs">무프로토콜 타임아웃 (1~10000 ms)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteCentralNoProtocolAsync(bool enable, int noProtocolTimeoutMs, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (noProtocolTimeoutMs < 1 || noProtocolTimeoutMs > 10000)
            throw new ArgumentOutOfRangeException(nameof(noProtocolTimeoutMs), "1~10000 ms");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_NO_PROTOCOL_SAVE,
            data: BuildNoProtocol(enable, noProtocolTimeoutMs), timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Output Interface / PHYS R/W ─────────────────────────

    /// <summary>출력 인터페이스 비트마스크를 읽는다. (<c>0x19</c>, bit0=RS232 / bit1=USB→Serial)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>인터페이스 비트마스크</returns>
    public async Task<byte> BleReadOutputInterfaceAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_OUTPUT_INTERFACE_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? data[0] : (byte)0;
    }

    /// <summary>출력 인터페이스 비트마스크를 설정한다. (<c>0x1A</c>)</summary>
    /// <param name="ifaceMask">인터페이스 비트마스크 (bit0=RS232 / bit1=USB→Serial)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteOutputInterfaceAsync(byte ifaceMask, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_OUTPUT_INTERFACE_SAVE,
            data: new[] { ifaceMask }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE PHY 설정 인덱스를 읽는다. (<c>0x1B</c>, 0=Auto / 1=1M / 2=2M)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>PHY 인덱스 (0=Auto / 1=1M / 2=2M)</returns>
    public async Task<byte> BleReadPhysAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_CENTRAL_PHYS_UPDATE_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? data[0] : (byte)0;
    }

    /// <summary>BLE PHY 설정 인덱스를 저장한다. (<c>0x1C</c>, 0=Auto / 1=1M / 2=2M)</summary>
    /// <param name="phyIndex">PHY 인덱스 (0=Auto / 1=1M / 2=2M)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWritePhysAsync(byte phyIndex, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_PHYS_UPDATE_SAVE,
            data: new[] { phyIndex }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Central / Peripheral Enable, RSSI, Disconnect ───────

    /// <summary>BLE Central 모드를 활성/비활성한다. (<c>0x21</c>)</summary>
    /// <param name="enable">활성 여부</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteCentralEnableAsync(bool enable, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_ENABLE_WRITE,
            data: new[] { (byte)(enable ? 1 : 0) }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE Peripheral 모드를 읽는다. (<c>0x30</c>, 0=Disable / 1=SPP)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>Peripheral 모드 (0=Disable / 1=SPP)</returns>
    public async Task<byte> BleReadPeripheralEnableAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_PERIPHERAL_ENABLE_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? data[0] : (byte)0;
    }

    /// <summary>BLE Peripheral 모드를 설정한다. (<c>0x31</c>, 0=Disable / 1=SPP — 화면에는 Disable/SPP 만 노출)</summary>
    /// <param name="mode">Peripheral 모드 (0=Disable / 1=SPP)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWritePeripheralEnableAsync(byte mode, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_ENABLE_WRITE,
            data: new[] { mode }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>현재 연결된 Central 링크의 RSSI 를 읽는다. (<c>0x25</c>, dBm)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>RSSI (dBm, sbyte)</returns>
    public async Task<sbyte> BleReadCentralConnectedRssiAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_CENTRAL_CONNECTED_RSSI_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? (sbyte)data[0] : (sbyte)0;
    }

    /// <summary>매칭된 Central 장치의 RSSI 를 읽는다. (<c>0x26</c>, dBm)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>RSSI (dBm, sbyte)</returns>
    public async Task<sbyte> BleReadCentralMatchedRssiAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_CENTRAL_MATCHED_RSSI_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? (sbyte)data[0] : (sbyte)0;
    }

    /// <summary>현재 연결된 Peripheral 링크의 RSSI 를 읽는다. (<c>0x36</c>, dBm)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>RSSI (dBm, sbyte)</returns>
    public async Task<sbyte> BleReadPeripheralConnectRssiAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_PERIPHERAL_CONNECT_RSSI_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? (sbyte)data[0] : (sbyte)0;
    }

    /// <summary>Central UUID 연결을 해제한다. (<c>0x24</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleCentralUuidDisconnectAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_UUID_DISCONNECT,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Peripheral 연결을 해제한다. (<c>0x39</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BlePeripheralDisconnectAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_DISCONNECT,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Bluetooth Card R/W ──────────────────────────────────

    /// <summary>BLE Card 사용 설정을 읽는다. (<c>0x70</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>활성 여부</returns>
    public async Task<bool> BleReadBleCardUsingAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_BLECARD_USING_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 && data[0] != 0;
    }

    /// <summary>BLE Card 사용 여부를 설정한다. (<c>0x71</c>)</summary>
    /// <param name="enable">활성 여부</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteBleCardUsingAsync(bool enable, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_BLECARD_USING_SAVE,
            data: new[] { (byte)(enable ? 1 : 0) }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>BLE Card RSSI 임계값(절대값)을 읽는다. (<c>0x72</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>RSSI 임계값 (절대값, 0~200)</returns>
    public async Task<byte> BleReadBleCardRssiAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG,
            Constants.BLE_CFG_BLECARD_RSSI_READ, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 ? data[0] : (byte)0;
    }

    /// <summary>BLE Card RSSI 임계값(절대값)을 설정한다. (<c>0x73</c>, 최대 200)</summary>
    /// <param name="rssiAbs">RSSI 임계값 (절대값, 0~200)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleWriteBleCardRssiAsync(byte rssiAbs, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (rssiAbs > 200)
            throw new ArgumentOutOfRangeException(nameof(rssiAbs), "0~200");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_BLECARD_RSSI_SAVE,
            data: new[] { rssiAbs }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Central / Peripheral UUID R/W (0x22/0x23, 0x32/0x33) ─

    /// <summary>Central UUID 5필드(20 byte)를 읽는다. (<c>0x22</c>)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>20 byte: <c>[RxD(2)][TxD(2)][BaseP1(2)][BaseP2(2)][BaseP3(12)]</c></returns>
    public async Task<byte[]> BleReadCentralUuidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_UUID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Central UUID 5필드(20 byte)를 저장한다. (<c>0x23</c>)</summary>
    /// <param name="uuid20">20 byte: <c>[RxD(2)][TxD(2)][BaseP1(2)][BaseP2(2)][BaseP3(12)]</c></param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException"><paramref name="uuid20"/> 가 20 byte 가 아님</exception>
    public async Task BleWriteCentralUuidAsync(byte[] uuid20, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (uuid20 is null || uuid20.Length != 20)
            throw new ArgumentException("UUID 는 20 byte 여야 합니다 (RxD2+TxD2+Base2+2+12).", nameof(uuid20));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_UUID_SAVE,
            data: uuid20, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Peripheral UUID 5필드(20 byte)를 읽는다. (<c>0x32</c>, Central UUID 구조 동일)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>20 byte UUID 구조</returns>
    public async Task<byte[]> BleReadPeripheralUuidAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_UUID_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>Peripheral UUID 5필드(20 byte)를 저장한다. (<c>0x33</c>)</summary>
    /// <param name="uuid20">20 byte UUID 구조 (Central 과 동일)</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException"><paramref name="uuid20"/> 가 20 byte 가 아님</exception>
    public async Task BleWritePeripheralUuidAsync(byte[] uuid20, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (uuid20 is null || uuid20.Length != 20)
            throw new ArgumentException("UUID 는 20 byte 여야 합니다 (RxD2+TxD2+Base2+2+12).", nameof(uuid20));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_UUID_SAVE,
            data: uuid20, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Central Connect Type R/W (0x2D/0x2E) ────────────────

    /// <summary>
    /// Central 자동 연결 방식을 읽는다. (<c>0x2D</c>)
    /// <para>type 0=UUID 매칭 자동, 1=User 수동, 2=MAC 매칭 자동(이때만 MAC 6 byte 동반).</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>(Type, Mac) — type!=2 이면 Mac 은 빈 배열</returns>
    public async Task<(byte Type, byte[] Mac)> BleReadCentralConnectTypeAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_CONNECT_PARAMS_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        byte type = data.Length >= 1 ? data[0] : (byte)0;
        byte[] mac = (type == 2 && data.Length >= 7) ? Slice(data, 1, 6) : Array.Empty<byte>();
        return (type, mac);
    }

    /// <summary>
    /// Central 자동 연결 방식을 설정한다. (<c>0x2E</c>)
    /// </summary>
    /// <param name="type">0=UUID 매칭 자동, 1=User 수동, 2=MAC 매칭 자동</param>
    /// <param name="mac">type==2 일 때 필수, 6 byte MAC. 그 외에는 무시</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> 가 0~2 가 아님</exception>
    /// <exception cref="ArgumentException">type==2 인데 <paramref name="mac"/> 가 6 byte 가 아님</exception>
    public async Task BleWriteCentralConnectTypeAsync(byte type, byte[]? mac = null, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (type > 2)
            throw new ArgumentOutOfRangeException(nameof(type), "0=UUID 자동 / 1=User / 2=MAC 자동");
        byte[] data;
        if (type == 2)
        {
            if (mac is null || mac.Length != 6)
                throw new ArgumentException("type=2(MAC 매칭)는 6 byte MAC 이 필요합니다.", nameof(mac));
            data = new byte[7];
            data[0] = type;
            Array.Copy(mac, 0, data, 1, 6);
        }
        else
        {
            data = new[] { type };
        }
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_CONNECT_PARAMS_WRITE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Central Matched Connect (0x2C) ──────────────────────

    /// <summary>스캔 목록에서 선택한 MAC 주소로 연결을 시도한다. (<c>0x2C</c>)</summary>
    /// <param name="mac">6 byte MAC 주소</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException"><paramref name="mac"/> 가 6 byte 가 아님</exception>
    public async Task BleCentralMatchedConnectAsync(byte[] mac, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (mac is null || mac.Length != 6)
            throw new ArgumentException("MAC 주소는 6 byte 여야 합니다.", nameof(mac));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_MATCHED_CONNECT,
            data: mac, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Send Command / Send Data (Central 0x2B/0x40, Peripheral 0x35/0x41) ─

    /// <summary>
    /// 연결된 Peripheral 로 데이터를 <b>프로토콜 프레임으로 감싸</b> 전달한다. (Central Send Command, <c>0x2B</c>)
    /// <para>데이터가 255 byte 를 초과하면 PC/SC 채널에서 자동으로 Extended APDU 로 인코딩된다.</para>
    /// </summary>
    /// <param name="data">전달할 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleCentralSendCommandAsync(byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data is null) throw new ArgumentNullException(nameof(data));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_SEND_DATA,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 연결된 Peripheral 로 데이터를 <b>raw 그대로</b> 전달한다. (Central Send Data, <c>0x40</c>)
    /// <para>데이터가 255 byte 를 초과하면 PC/SC 채널에서 자동으로 Extended APDU 로 인코딩된다.</para>
    /// </summary>
    /// <param name="data">전달할 raw 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BleCentralSendDataAsync(byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data is null) throw new ArgumentNullException(nameof(data));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_SEND_DATA_RAW,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 연결된 Central 로 데이터를 <b>프로토콜 프레임으로 감싸</b> 전달한다. (Peripheral Send Command, <c>0x35</c>)
    /// </summary>
    /// <param name="data">전달할 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BlePeripheralSendCommandAsync(byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data is null) throw new ArgumentNullException(nameof(data));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_SEND_DATA,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 연결된 Central 로 데이터를 <b>raw 그대로</b> 전달한다. (Peripheral Send Data, <c>0x41</c>)
    /// </summary>
    /// <param name="data">전달할 raw 데이터</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    public async Task BlePeripheralSendDataAsync(byte[] data, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (data is null) throw new ArgumentNullException(nameof(data));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_PERIPHERAL_SEND_DATA_RAW,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Bluetooth Security: Levels (0x60/0x61) ──────────────

    /// <summary>
    /// BLE 페어링 보안 레벨을 읽는다. (<c>0x60</c>)
    /// <para>1=암호화(인증 없음), 2=인증 암호화, 3=Key matching, 4=LE Secure(OOB).</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>(Level, Key) — Level 3/4 일 때 Key(ASCII 6 / OOB 16)가 동반될 수 있음. 그 외 빈 배열</returns>
    public async Task<(byte Level, byte[] Key)> BleReadSecurityLevelAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_SECURITY_LEVELS_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        byte level = data.Length >= 1 ? data[0] : (byte)0;
        byte[] key = data.Length > 1 ? Slice(data, 1, data.Length - 1) : Array.Empty<byte>();
        return (level, key);
    }

    /// <summary>
    /// BLE 보안 레벨 1(암호화, 인증 없음) 또는 2(인증 암호화)를 설정한다. (<c>0x61</c>)
    /// <para>레벨 3/4 는 <see cref="BleWriteSecurityLevelKeyMatchingAsync"/> /
    /// <see cref="BleWriteSecurityLevelLeSecureAsync"/> 를 사용할 것.</para>
    /// </summary>
    /// <param name="level">1=암호화(인증 없음), 2=인증 암호화</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="level"/> 가 1 또는 2 가 아님</exception>
    public async Task BleWriteSecurityLevelAsync(byte level, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (level != 1 && level != 2)
            throw new ArgumentOutOfRangeException(nameof(level), "레벨 3/4 는 전용 메서드를 사용하세요 (1 또는 2만 허용).");
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_SECURITY_LEVELS_WRITE,
            data: new[] { level }, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// BLE 보안 레벨 3(Key matching, 6자리 ASCII 키)을 설정한다. (<c>0x61</c>, <c>[03][asciiKey(6)]</c>)
    /// </summary>
    /// <param name="asciiKey6">6자리 ASCII 매칭 키</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException"><paramref name="asciiKey6"/> 가 6자리 ASCII 가 아님</exception>
    public async Task BleWriteSecurityLevelKeyMatchingAsync(string asciiKey6, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] keyBytes = Encoding.ASCII.GetBytes(asciiKey6 ?? string.Empty);
        if (keyBytes.Length != 6)
            throw new ArgumentException("Key matching 키는 6자리 ASCII 여야 합니다.", nameof(asciiKey6));
        byte[] data = new byte[7];
        data[0] = 3;
        Array.Copy(keyBytes, 0, data, 1, 6);
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_SECURITY_LEVELS_WRITE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// BLE 보안 레벨 4(LE Secure, 16 byte OOB 키)를 설정한다. (<c>0x61</c>, <c>[04][oobKey(16)]</c>)
    /// </summary>
    /// <param name="oobKey16">16 byte OOB 키</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException"><paramref name="oobKey16"/> 가 16 byte 가 아님</exception>
    public async Task BleWriteSecurityLevelLeSecureAsync(byte[] oobKey16, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (oobKey16 is null || oobKey16.Length != 16)
            throw new ArgumentException("OOB 키는 16 byte 여야 합니다.", nameof(oobKey16));
        byte[] data = new byte[17];
        data[0] = 4;
        Array.Copy(oobKey16, 0, data, 1, 16);
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_SECURITY_LEVELS_WRITE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Bluetooth Security: Communication Data (0x1D/0x1E) ──

    /// <summary>
    /// 통신 데이터(내부 명령 사용) 설정을 읽는다. (<c>0x1D</c>)
    /// <para>Enable=1 이면 Challenge-Response 인증 후 master key 로 BLE 명령을 사용한다.</para>
    /// <para>⚠ AES 키는 USB/UART 응답에만 포함되고 BLE 무선 응답에서는 제외된다.</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>(Enable, AesKey) — Enable=1 이고 물리 채널이면 AesKey(16)가 동반될 수 있음. 그 외 빈 배열</returns>
    public async Task<(bool Enable, byte[] AesKey)> BleReadCommunicationDataAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_INPUT_PROTOCOL_READ,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        bool enable = data.Length >= 1 && data[0] != 0;
        byte[] key = data.Length >= 17 ? Slice(data, 1, 16) : Array.Empty<byte>();
        return (enable, key);
    }

    /// <summary>
    /// 통신 데이터(내부 명령 사용) 설정을 저장한다. (<c>0x1E</c>)
    /// <para>Enable=true 면 16 byte master key(AES)를 함께 저장한다.</para>
    /// </summary>
    /// <param name="enable">내부 명령(인증 후 사용) 활성 여부</param>
    /// <param name="aesKey16">Enable=true 일 때 필수, 16 byte AES master key</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException">Enable=true 인데 <paramref name="aesKey16"/> 가 16 byte 가 아님</exception>
    public async Task BleWriteCommunicationDataAsync(bool enable, byte[]? aesKey16 = null, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data;
        if (enable)
        {
            if (aesKey16 is null || aesKey16.Length != 16)
                throw new ArgumentException("Enable=true 이면 16 byte AES master key 가 필요합니다.", nameof(aesKey16));
            data = new byte[17];
            data[0] = 1;
            Array.Copy(aesKey16, 0, data, 1, 16);
        }
        else
        {
            data = new byte[] { 0 };
        }
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_CENTRAL_INPUT_PROTOCOL_SAVE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── Bluetooth Security: Challenge-Response (0x64/0x65/0x66) ─

    /// <summary>
    /// 인증용 challenge 16 byte 를 가져온다. (<c>0x64</c> Get Random)
    /// <para>리더기가 master key 로 암호화한 challenge 를 회신한다.</para>
    /// </summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns>16 byte challenge</returns>
    public async Task<byte[]> BleSecurityGetRandomAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_USER_SECURITY_RANDOM,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 계산한 response 32 byte 를 전송하여 인증한다. (<c>0x65</c> Authentication)
    /// </summary>
    /// <param name="response32">master key 로 계산한 32 byte 응답</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException"><paramref name="response32"/> 가 32 byte 가 아님</exception>
    public async Task BleSecurityAuthenticateAsync(byte[] response32, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (response32 is null || response32.Length != 32)
            throw new ArgumentException("response 는 32 byte 여야 합니다.", nameof(response32));
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_USER_SECURITY_AUTH,
            data: response32, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    /// <summary>인증 결과를 확인한다. (<c>0x66</c> Auth State Read)</summary>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <returns><c>true</c>=인증 성공(1), <c>false</c>=실패(0)</returns>
    public async Task<bool> BleReadSecurityAuthStateAsync(int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        byte[] data = await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_USER_SECURITY_AUTH_STATE,
            timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
        return data.Length >= 1 && data[0] == 1;
    }

    // ─── Bluetooth Card: Key Save (0x74) ─────────────────────

    /// <summary>
    /// BLE 카드의 첫 키(16 byte)와 Custom UUID(16 byte)를 저장한다. (<c>0x74</c>, Save 전용 32 byte)
    /// <para>보안을 위해 Read 명령은 제공되지 않는다.</para>
    /// </summary>
    /// <param name="firstKey16">첫 키 16 byte</param>
    /// <param name="customUuid16">Custom UUID 16 byte</param>
    /// <param name="timeoutMs">응답 대기 최대 시간 (ms)</param>
    /// <param name="ct">취소 토큰</param>
    /// <exception cref="ArgumentException">키 또는 UUID 가 16 byte 가 아님</exception>
    public async Task BleWriteBleCardKeyAsync(byte[] firstKey16, byte[] customUuid16, int timeoutMs = 1000, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (firstKey16 is null || firstKey16.Length != 16)
            throw new ArgumentException("firstKey 는 16 byte 여야 합니다.", nameof(firstKey16));
        if (customUuid16 is null || customUuid16.Length != 16)
            throw new ArgumentException("customUUID 는 16 byte 여야 합니다.", nameof(customUuid16));
        byte[] data = new byte[32];
        Array.Copy(firstKey16, 0, data, 0, 16);
        Array.Copy(customUuid16, 0, data, 16, 16);
        await SendCommandAsync(Constants.MAJOR_BLE_CONFIG, Constants.BLE_CFG_BLECARD_KEY_SAVE,
            data: data, timeoutMs: timeoutMs, ct: ct).ConfigureAwait(false);
    }

    // ─── 16-bit 인코딩/디코딩 헬퍼 ───────────────────────────

    // net472 호환: 배열 range/slice 연산자(System.Range) 미지원 → Array.Copy 기반 헬퍼 사용.
    private static byte[] Slice(byte[] src, int offset, int length)
    {
        var r = new byte[length];
        if (length > 0) Array.Copy(src, offset, r, 0, length);
        return r;
    }

    private static byte[] U16Be(int v) => new[] { (byte)((v >> 8) & 0xFF), (byte)(v & 0xFF) };

    private static void WriteU16Le(byte[] buf, int offset, int v)
    {
        buf[offset]     = (byte)(v & 0xFF);
        buf[offset + 1] = (byte)((v >> 8) & 0xFF);
    }

    private static int ReadU16Be(byte[] data, int offset)
        => data.Length >= offset + 2 ? (data[offset] << 8) | data[offset + 1] : 0;

    private static (bool Enable, int TimeoutMs) ParseNoProtocol(byte[] data)
    {
        bool enable = data.Length >= 1 && data[0] != 0;
        int t = data.Length >= 3 ? (data[1] << 8) | data[2] : 0;
        return (enable, t);
    }

    private static byte[] BuildNoProtocol(bool enable, int timeoutMs)
        => new[] { (byte)(enable ? 1 : 0), (byte)((timeoutMs >> 8) & 0xFF), (byte)(timeoutMs & 0xFF) };

    // ─── 유틸리티 ────────────────────────────────────────────

    /// <summary>RF 파워 인덱스를 dBm 문자열로 변환한다. (프로토콜 RF Power 인덱스 표 0x14/0x15)</summary>
    /// <param name="powerIndex">RF 파워 인덱스 바이트 (0~8)</param>
    public static string TxPowerToString(byte powerIndex) => powerIndex switch
    {
        0 => "-40 dBm",
        1 => "-20 dBm",
        2 => "-16 dBm",
        3 => "-12 dBm",
        4 => "-8 dBm",
        5 => "-4 dBm",
        6 => "0 dBm",
        7 => "4 dBm",   // 기본값
        8 => "8 dBm",
        _ => $"? ({powerIndex})"
    };
}
