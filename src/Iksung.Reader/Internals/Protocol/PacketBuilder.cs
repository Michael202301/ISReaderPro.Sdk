namespace Iksung.Reader.Internals.Protocol;

internal static class PacketBuilder
{
    public const byte STX = 0x01;
    public const byte ETX = 0x03;

    // Request packet (Host -> Device): STX CMD1 CMD2 LEN_H LEN_L Data CS ETX  (no State byte)
    public static byte[] BuildPacket(byte cmd1, byte cmd2, byte[]? data = null)
    {
        int dataLen = data?.Length ?? 0;
        byte[] packet = new byte[7 + dataLen];
        packet[0] = STX;
        packet[1] = cmd1;
        packet[2] = cmd2;
        packet[3] = (byte)((dataLen >> 8) & 0xFF);
        packet[4] = (byte)(dataLen & 0xFF);
        if (dataLen > 0) Array.Copy(data!, 0, packet, 5, dataLen);
        int crcIndex = 5 + dataLen;
        packet[crcIndex]     = CalcChecksum(packet, 1, crcIndex - 1);
        packet[crcIndex + 1] = ETX;
        return packet;
    }

    public static byte CalcChecksum(byte[] buffer, int start, int count)
    {
        byte sum = 0;
        for (int i = start; i < start + count; i++) sum += buffer[i];
        return sum;
    }

    // Modbus RTU CRC16 — returns big-endian (hi byte in MSB) for direct comparison with on-wire bytes.
    public static ushort ModbusCrc16(byte[] buffer, int length)
    {
        ushort crc = 0xFFFF;
        for (int pos = 0; pos < length; pos++)
        {
            crc ^= buffer[pos];
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0) { crc >>= 1; crc ^= 0xA001; }
                else                       { crc >>= 1; }
            }
        }
        return (ushort)((crc >> 8) | (crc << 8));
    }
}
