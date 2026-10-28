namespace FitnessTracker.Client.Services;

/// <summary>Holds the bearer token for the session and remembers it across app starts.</summary>
public sealed class AuthStore(LocalStore store)
{
    private const string TokenKey = "tracker.token";

    private string? _token;
    private bool _loaded;

    public bool HasToken => !string.IsNullOrEmpty(_token);

    public async Task<string?> GetTokenAsync()
    {
        if (!_loaded)
        {
            _token = await store.GetAsync(TokenKey);
            _loaded = true;
        }

        return _token;
    }

    public async Task SetTokenAsync(string token)
    {
        _token = token;
        _loaded = true;
        await store.SetAsync(TokenKey, token);
    }

    public async Task ClearAsync()
    {
        _token = null;
        _loaded = true;
        await store.RemoveAsync(TokenKey);
    }
}
