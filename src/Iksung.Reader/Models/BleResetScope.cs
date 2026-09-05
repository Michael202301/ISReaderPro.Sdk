namespace Iksung.Reader;

/// <summary>
/// BLE SYSTEM_RESET(<c>0x18</c>) 의 재시작 범위. (펌웨어 V1.37 BLE_Passthrough_Protocol.md §11)
/// <para>정의되지 않은 값을 보내면 펌웨어가 <c>state=0xFF</c>(실패) 로 응답한다.</para>
/// </summary>
public enum BleResetScope : byte
{
    /// <summary>MCU 전체 재부팅 (~1s). 모든 BLE 연결 해제 — 응답 직후 재부팅하므로 호스트는 재연결 대기 필요.</summary>
    Full = 0x00,

    /// <summary>Central 만 재시작 (~100ms). Peripheral 연결 유지.</summary>
    CentralOnly = 0x01,

    /// <summary>Peripheral 만 재시작 (~100ms). Central 연결 유지.</summary>
    PeripheralOnly = 0x02,

    /// <summary>EEPROM→RAM 설정 재로드만 (~10ms). 연결 모두 유지 (일부 설정은 적용에 재연결이 필요할 수 있음).</summary>
    SettingsReload = 0x03,
}
