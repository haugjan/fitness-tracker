using Microsoft.JSInterop;

namespace FitnessTracker.Client.Services;

/// <summary>
/// Lets the app tell the user that a new version waits in the service worker, instead of
/// updating under their hands while they type.
/// </summary>
public sealed class PwaUpdateService(IJSRuntime js) : IAsyncDisposable
{
    private DotNetObjectReference<PwaUpdateService>? _reference;

    public bool UpdateAvailable { get; private set; }

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_reference is not null)
        {
            return;
        }

        _reference = DotNetObjectReference.Create(this);

        try
        {
            await js.InvokeVoidAsync("trackerPwa.init", _reference);
        }
        catch (JSException)
        {
            // No service worker in this browser, nothing to announce.
        }
    }

    [JSInvokable]
    public void OnUpdateAvailable()
    {
        UpdateAvailable = true;
        Changed?.Invoke();
    }

    public async Task ApplyUpdateAsync()
    {
        try
        {
            await js.InvokeVoidAsync("trackerPwa.applyUpdate");
        }
        catch (JSException)
        {
            // The page reloads on the next start instead.
        }
    }

    public ValueTask DisposeAsync()
    {
        _reference?.Dispose();
        _reference = null;
        return ValueTask.CompletedTask;
    }
}
