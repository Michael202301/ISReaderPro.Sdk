namespace Iksung.Reader.Internals.Crypto;

// IEEE 802.3 CRC-32 — V4.0 부트로더 Program Data 응답 검증용.
// V5.15 ISUpdate/crc32.cs 포팅 (table 기반, polynomial 0xEDB88320).
internal static class Crc32
{
    private static readonly uint[] _table = BuildTable();

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : (c >> 1);
            t[i] = c;
        }
        return t;
    }

    // V5.15 와 동일: 시작값 0xFFFFFFFF, 종료 시 1's complement.
    public static uint Calculate(byte[] buffer, int offset, int length)
    {
        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < length; i++)
            crc = _table[(crc ^ buffer[offset + i]) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }
}
