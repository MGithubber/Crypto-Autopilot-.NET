namespace Application.Interfaces.Services;

public interface IStrategyEngine : IDisposable, IAsyncDisposable
{
    public Guid Guid { get; }

    public Task StartTradingAsync();

    public bool IsRunning();

    public Task StopTradingAsync();
}
