namespace Avae.ViewModels;

internal sealed class NavigationGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AsyncLocal<bool> _inProgress = new();

    public async Task EnterAsync()
    {
        if (_inProgress.Value)
            throw new InvalidOperationException("Reentrant navigation is not supported.");

        await _gate.WaitAsync();
        _inProgress.Value = true;
    }

    public void Exit()
    {
        _inProgress.Value = false;
        _gate.Release();
    }
}
