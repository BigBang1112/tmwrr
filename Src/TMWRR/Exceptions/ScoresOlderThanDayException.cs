namespace TMWRR.Exceptions;

[Serializable]
public class ScoresOlderThanDayException : Exception
{
	public ScoresOlderThanDayException() : this("The scores are older than a day. The scores will be checked again.") { }
	public ScoresOlderThanDayException(string message) : base(message) { }
	public ScoresOlderThanDayException(string message, Exception inner) : base(message, inner) { }
}
