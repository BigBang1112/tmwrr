using System.Text;

namespace TMWRR.Api;

public class TmwrrProblemException : Exception
{
    public ProblemDetails? Problem { get; }

    public TmwrrProblemException(ProblemDetails problem, HttpRequestException innerException)
        : base($"{problem.Title}{ConcatErrors(problem.Errors)}", innerException)
    {
        Problem = problem;
    }

    public TmwrrProblemException(string message, HttpRequestException innerException)
        : base(message, innerException)
    {

    }

    private static string ConcatErrors(Dictionary<string, string[]> errors)
    {
        if (errors is null or { Count: 0 })
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var kvp in errors)
        {
            sb.Append($"\n{kvp.Key}: {string.Join(", ", kvp.Value)}");
        }

        return sb.ToString();
    }
}