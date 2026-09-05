namespace Iksung.Reader.Exceptions;

/// <summary>Base exception for all IKSUNG reader errors.</summary>
public class IksungException : Exception
{
    /// <inheritdoc/>
    public IksungException(string message) : base(message) { }
    /// <inheritdoc/>
    public IksungException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>The physical communication channel disconnected unexpectedly.</summary>
public sealed class ChannelDisconnectedException : IksungException
{
    /// <inheritdoc/>
    public ChannelDisconnectedException(string message) : base(message) { }
}

/// <summary>The reader did not respond within the allotted time.</summary>
public sealed class IksungTimeoutException : IksungException
{
    /// <inheritdoc/>
    public IksungTimeoutException(string message) : base(message) { }
}

/// <summary>The reader returned a protocol-level error (STATE_FAIL or malformed response).</summary>
public sealed class IksungProtocolException : IksungException
{
    /// <inheritdoc/>
    public IksungProtocolException(string message) : base(message) { }
}
