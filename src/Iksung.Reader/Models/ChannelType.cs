namespace Iksung.Reader;

/// <summary>리더기 연결 채널 종류.</summary>
public enum ChannelType
{
    /// <summary>시리얼 포트 (System.IO.Ports)</summary>
    Serial,
    /// <summary>FTDI D2XX 직접 USB</summary>
    Ftdi,
    /// <summary>PC/SC (winscard / pcsc-lite)</summary>
    Pcsc,
    /// <summary>TCP/IP 소켓</summary>
    Socket,
}
