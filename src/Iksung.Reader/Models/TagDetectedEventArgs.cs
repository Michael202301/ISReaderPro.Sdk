namespace Iksung.Reader;

/// <summary>AutoRead 모드에서 카드/태그가 감지됐을 때 발생하는 이벤트 인자.</summary>
public sealed class TagDetectedEventArgs : EventArgs
{
    /// <summary>감지된 카드/태그 종류.</summary>
    public CardType CardType { get; }

    /// <summary>UID 바이트 배열.</summary>
    public byte[] Uid { get; }

    /// <summary>UID를 16진수 문자열로 반환 (하이픈 없음, 대문자).</summary>
    public string UidHex => BitConverter.ToString(Uid).Replace("-", "");

    /// <summary>응답 cmd1 (MAJOR 커맨드 바이트).</summary>
    public byte Cmd1 { get; }

    /// <summary>응답 cmd2 (MINOR 커맨드 바이트).</summary>
    public byte Cmd2 { get; }

    /// <summary>펌웨어가 반환한 전체 Data 페이로드.</summary>
    public byte[] RawData { get; }

    internal TagDetectedEventArgs(CardType cardType, byte[] uid, byte cmd1, byte cmd2, byte[] rawData)
    {
        CardType = cardType;
        Uid      = uid;
        Cmd1     = cmd1;
        Cmd2     = cmd2;
        RawData  = rawData;
    }
}
