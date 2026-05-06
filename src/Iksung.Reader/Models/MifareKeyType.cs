namespace Iksung.Reader;

/// <summary>Mifare Classic 인증에 사용하는 키 타입.</summary>
public enum MifareKeyType : byte
{
    /// <summary>Key A (읽기/쓰기 제어)</summary>
    KeyA = 0x01,

    /// <summary>Key B (쓰기 전용 또는 사용자 정의)</summary>
    KeyB = 0x02,
}
