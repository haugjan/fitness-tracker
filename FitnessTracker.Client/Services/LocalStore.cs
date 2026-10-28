using Microsoft.JSInterop;

namespace FitnessTracker.Client.Services;

/// <summary>Thin wrapper over the browser store defined in index.html.</summary>
public sealed class LocalStore(IJSRuntime js)
{
    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("tracker.get", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("tracker.set", key, value);
        }
        catch (JSException)
        {
            // A browser with blocked storage still works, it just forgets the sign in.
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await js.InvokeVoidAsync("tracker.remove", key);
        }
        catch (JSException)
        {
            // Nothing to clean up when the store is unavailable.
        }
    }
}
