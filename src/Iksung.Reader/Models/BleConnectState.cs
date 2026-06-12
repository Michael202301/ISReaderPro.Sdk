namespace Iksung.Reader;

/// <summary>
/// BLE 연결 상태 진단 결과. (펌웨어 V1.37 BLE_Passthrough_Protocol.md §12)
/// <para>
/// Central(<see cref="IksungReader.BleReadCentralConnectStateAsync"/>) /
/// Peripheral(<see cref="IksungReader.BleReadPeripheralConnectStateAsync"/>) Connect State Read 응답을 담는다.
/// </para>
/// <para>
/// ⚠ V1.36 → V1.37 의미 변화: V1.36 Central <c>state==2</c> 는 "전송 가능" 이었으나
/// V1.37 부터 <c>state==2</c> 는 "MTU 교환 중(전송 불가)" 이다.
/// 데이터 전송 가능 여부는 반드시 <see cref="TxReady"/> (또는 <see cref="State"/>==3) 로 판단할 것.
/// </para>
/// </summary>
public readonly struct BleConnectState
{
    /// <summary>
    /// 연결 상태 코드.
    /// <list type="bullet">
    /// <item><description><c>0x00</c> 미연결(idle)</description></item>
    /// <item><description><c>0x01</c> 페어링/GATT 진행 중</description></item>
    /// <item><description><c>0x02</c> MTU 교환 진행 중 (아직 전송 불가)</description></item>
    /// <item><description><c>0x03</c> 완전 준비 (TX 가능)</description></item>
    /// <item><description><c>0xFE</c> 에러 (마지막 페어링 실패)</description></item>
    /// </list>
    /// </summary>
    public byte State { get; }

    /// <summary><c>true</c> = 즉시 데이터 전송 가능 (<see cref="State"/>==3 과 동치). 호스트는 이 값으로 전송 가능 판단을 권장.</summary>
    public bool TxReady { get; }

    /// <summary>마지막 페어링 실패 등 에러 상태인지 여부 (<see cref="State"/>==0xFE).</summary>
    public bool IsError => State == 0xFE;

    /// <summary>BLE 링크가 연결되었는지 여부. (실제 TX 가능 여부는 <see cref="TxReady"/> 로 판단)</summary>
    public bool IsConnected => State >= 0x01 && State != 0xFE;

    /// <summary>구조체를 생성한다.</summary>
    /// <param name="state">연결 상태 코드</param>
    /// <param name="txReady">즉시 전송 가능 여부</param>
    public BleConnectState(byte state, bool txReady)
    {
        State   = state;
        TxReady = txReady;
    }

    /// <inheritdoc/>
    public override string ToString()
        => $"State=0x{State:X2}, TxReady={TxReady}, Connected={IsConnected}{(IsError ? ", Error" : "")}";
}
