namespace TMWRR.Services;

public interface IDelayService
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}

internal sealed class DelayService : IDelayService
{
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
    }
}
