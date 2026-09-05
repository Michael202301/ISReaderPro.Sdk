namespace Iksung.Reader;

/// <summary>AutoRead 이벤트 및 카드 활성화 결과로 반환되는 카드/태그 기술 타입.</summary>
public enum CardType
{
    /// <summary>인식되지 않은 카드 타입.</summary>
    Unknown,

    /// <summary>ISO 14443 Type A (Mifare 포함).</summary>
    Iso14443a,

    /// <summary>ISO 14443 Type B.</summary>
    Iso14443b,

    /// <summary>ISO 14443 Type A 또는 B (AutoRead 혼합 감지 시).</summary>
    Iso14443ab,

    /// <summary>ISO 15693 vicinity 카드 (iCODE, Tag-it HF 등).</summary>
    Iso15693,

    /// <summary>Mifare Classic 1K / 4K.</summary>
    MifareClassic,

    /// <summary>Mifare Ultralight / NTag 시리즈.</summary>
    MifareUltralight,

    /// <summary>Mifare DESFire EV1/EV2/EV3.</summary>
    MifareDesfire,

    /// <summary>FeliCa (Sony, ISO 18092).</summary>
    Felica,

    /// <summary>LF 125 kHz (EM410X, HID 등).</summary>
    Lf125Khz,
}
