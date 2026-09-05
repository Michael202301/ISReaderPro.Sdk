using System.Text;
using Moq;
using Xunit;
using Iksung.Reader.Exceptions;
using Iksung.Reader.Internals.Channels;
using Iksung.Reader.Internals.Protocol;

namespace Iksung.Reader.Tests;

public class IksungReaderTests
{
    // Helper: build a response byte[] whose Data section is the given ASCII string.
    private static byte[] AsciiPayload(string text) => Encoding.ASCII.GetBytes(text);

    [Fact]
    public async Task ReadVersionAsync_ReturnsVersionString()
    {
        var channelMock = new Mock<IIksungChannel>();
        channelMock.Setup(c => c.IsConnected).Returns(true);

        byte[] expectedPayload = AsciiPayload("IS-3500K_V1.8");
        channelMock
            .Setup(c => c.SendAndReceiveAsync(
                It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPayload);

        // Use internal constructor via reflection (InternalsVisibleTo lets tests access internals)
        var reader = CreateReader(channelMock.Object, ChannelType.Serial);

        string version = await reader.ReadVersionAsync();

        Assert.Equal("IS-3500K_V1.8", version);
        channelMock.Verify(c => c.SendAndReceiveAsync(
            It.Is<byte[]>(p => p[1] == Constants.MAJOR_COMMON && p[2] == Constants.COMMON_VERSION),
            1000,
            default), Times.Once);
    }

    [Fact]
    public async Task ReadVersionAsync_WhenTimeout_ThrowsIksungTimeoutException()
    {
        var channelMock = new Mock<IIksungChannel>();
        channelMock.Setup(c => c.IsConnected).Returns(true);
        channelMock
            .Setup(c => c.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IksungTimeoutException("timeout"));

        var reader = CreateReader(channelMock.Object, ChannelType.Serial);

        await Assert.ThrowsAsync<IksungTimeoutException>(() => reader.ReadVersionAsync());
    }

    [Fact]
    public async Task DisposeAsync_CallsDisconnect()
    {
        var channelMock = new Mock<IIksungChannel>();
        channelMock.Setup(c => c.IsConnected).Returns(true);

        var reader = CreateReader(channelMock.Object, ChannelType.Serial);
        await reader.DisposeAsync();

        channelMock.Verify(c => c.Disconnect(), Times.Once);
        channelMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public async Task SendRawCommandAsync_BuildsCorrectPacket()
    {
        var channelMock = new Mock<IIksungChannel>();
        channelMock.Setup(c => c.IsConnected).Returns(true);
        channelMock
            .Setup(c => c.SendAndReceiveAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var reader = CreateReader(channelMock.Object, ChannelType.Serial);

        await reader.SendRawCommandAsync(0x01, 0x20);

        channelMock.Verify(c => c.SendAndReceiveAsync(
            It.Is<byte[]>(p => p[0] == PacketBuilder.STX &&
                               p[1] == 0x01 && p[2] == 0x20 &&
                               p[p.Length - 1] == PacketBuilder.ETX),
            1000, default), Times.Once);
    }

    // Creates an IksungReader via the internal constructor (InternalsVisibleTo ensures access).
    private static IksungReader CreateReader(IIksungChannel channel, ChannelType type)
    {
        var ctor = typeof(IksungReader).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(IIksungChannel), typeof(ChannelType)],
            null)!;
        return (IksungReader)ctor.Invoke([channel, type]);
    }
}
