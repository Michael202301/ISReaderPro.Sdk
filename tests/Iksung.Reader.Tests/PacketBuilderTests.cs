using Xunit;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader.Tests;

public class PacketBuilderTests
{
    [Fact]
    public void BuildPacket_NoData_Has7Bytes()
    {
        byte[] pkt = PacketBuilder.BuildPacket(0x00, 0x10);
        Assert.Equal(7, pkt.Length);
        Assert.Equal(PacketBuilder.STX, pkt[0]);
        Assert.Equal(PacketBuilder.ETX, pkt[6]);
    }

    [Fact]
    public void BuildPacket_WithData_CorrectLength()
    {
        byte[] data = [0x01, 0x02, 0x03];
        byte[] pkt  = PacketBuilder.BuildPacket(0x00, 0x10, data);
        Assert.Equal(10, pkt.Length); // 7 + 3
    }

    [Fact]
    public void BuildPacket_Checksum_IsCorrect()
    {
        byte[] pkt = PacketBuilder.BuildPacket(0x00, 0x10);
        // checksum = sum of bytes[1..4]  (cmd1 cmd2 lenH lenL)
        byte expected = PacketBuilder.CalcChecksum(pkt, 1, 4);
        Assert.Equal(expected, pkt[5]);
    }

    [Fact]
    public void Constants_StxEtx_Match_PacketBuilder()
    {
        Assert.Equal(PacketBuilder.STX, Constants.STX);
        Assert.Equal(PacketBuilder.ETX, Constants.ETX);
    }

    [Fact]
    public void ModbusCrc16_ReturnsNonZero()
    {
        byte[] buf = [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A];
        ushort crc = PacketBuilder.ModbusCrc16(buf, buf.Length);
        Assert.NotEqual(0, crc);
    }
}
