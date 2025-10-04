namespace TMWRR.Api;

[Serializable]
public class TmwrrRateLimitException : Exception
{
    public TmwrrRateLimitException() { }
    public TmwrrRateLimitException(string message) : base(message) { }
    public TmwrrRateLimitException(string message, Exception inner) : base(message, inner) { }
}