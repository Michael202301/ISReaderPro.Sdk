namespace Iksung.Reader.Internals.Channels;

internal enum PacketProtocol : byte
{
    Standard  = 0x01,  // STX/ETX with cmd1/cmd2/state/lenH/lenL/data/CRC/ETX
    OldFormat = 0x02,  // 0x02 cmd1 state lenH lenL data CRC ETX  (no cmd2)
    ModbusRtu = 0x10,  // Modbus RTU response (function code 0x03), CRC16
    Invalid   = 0xFF,  // 위 3가지 어디에도 매칭 안되는 raw byte 들
}

internal sealed class IksungPacket(byte cmd1, byte cmd2, byte state, byte[] data,
                                   byte[]? lowData = null,
                                   PacketProtocol protocol = PacketProtocol.Standard)
{
    public byte          Cmd1     { get; } = cmd1;
    public byte          Cmd2     { get; } = cmd2;
    public byte          State    { get; } = state;
    public byte[]        Data     { get; } = data;
    public byte[]        LowData  { get; } = lowData ?? [];
    public PacketProtocol Protocol { get; } = protocol;
}
