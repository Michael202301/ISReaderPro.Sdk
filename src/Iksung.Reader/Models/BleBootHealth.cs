using System.Collections.Generic;

namespace Iksung.Reader;

/// <summary>
/// 부팅 진단(Boot Health) 결과. (펌웨어 V1.40 BLE_Passthrough_Protocol.md §13)
/// <para>
/// <see cref="IksungReader.BleReadBootHealthAsync"/> 로 조회. 24-byte 응답 중 앞 16-byte 가
/// 핵심 subsystem 의 init 결과(int8: <c>0</c>=OK, <c>!=0</c>=실패코드), 뒤 8-byte 는 reserved.
/// </para>
/// </summary>
public readonly struct BleBootHealth
{
    private readonly sbyte[] _results;

    /// <summary>
    /// 16개 subsystem 의 init 결과 (<c>0</c>=OK, <c>!=0</c>=실패코드, int8).
    /// 인덱스는 <see cref="Names"/> 와 1:1 대응한다.
    /// </summary>
    public IReadOnlyList<sbyte> Results => _results;

    /// <summary>모든 subsystem 이 정상(0)인지 여부.</summary>
    public bool AllOk
    {
        get
        {
            foreach (var r in _results)
                if (r != 0) return false;
            return true;
        }
    }

    /// <summary>16개 subsystem 이름. 인덱스 0~15 가 <see cref="Results"/> 와 1:1 대응.</summary>
    public static IReadOnlyList<string> Names { get; } = new[]
    {
        "eeprom", "eeprom_ble", "eeprom_acu", "wdt", "ble", "uart0", "uart1", "crypto",
        "ble_auth_key", "tim_tick", "led", "pn5180", "desfire", "usb", "wiegand", "threads",
    };

    /// <summary>구조체를 생성한다.</summary>
    /// <param name="results">16개 subsystem init 결과 배열</param>
    public BleBootHealth(sbyte[] results)
    {
        _results = results ?? System.Array.Empty<sbyte>();
    }

    /// <summary>실패한 subsystem 의 이름 목록을 반환한다. 모두 정상이면 빈 목록.</summary>
    public IReadOnlyList<string> FailedSubsystems
    {
        get
        {
            var list = new List<string>();
            for (int i = 0; i < _results.Length && i < Names.Count; i++)
                if (_results[i] != 0) list.Add(Names[i]);
            return list;
        }
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (AllOk) return "BootHealth: ALL OK";
        var failed = FailedSubsystems;
        return $"BootHealth: {failed.Count} FAIL ({string.Join(", ", failed)})";
    }
}
