using Microsoft.JSInterop;

namespace FitnessTracker.Client.Services;

/// <summary>Was der Browser über eine mögliche Installation sagt.</summary>
public sealed record InstallState(bool CanPrompt, bool IsStandalone, bool IsIos);

/// <summary>
/// Fragt beim Start, ob die Karte als App auf den Startbildschirm soll. Android und Desktop
/// können das über den Browser, auf iOS geht nur der Weg über das Teilen-Menü, darum dort
/// ein Hinweis statt eines Knopfs. Ein Nein wird gemerkt und nicht wiederholt.
/// </summary>
public sealed class InstallPrompt(IJSRuntime js, LocalStore store) : IAsyncDisposable
{
    private const string DismissedKey = "tracker.install.dismissed";

    private DotNetObjectReference<InstallPrompt>? _reference;
    private bool _dismissed;
    private bool _standalone;
    private bool _installed;

    public bool CanPrompt { get; private set; }

    public bool ShowIosHint { get; private set; }

    public bool Visible => !_dismissed && !_standalone && !_installed && (CanPrompt || ShowIosHint);

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_reference is not null)
        {
            return;
        }

        _dismissed = await store.GetAsync(DismissedKey) is not null;
        _reference = DotNetObjectReference.Create(this);

        try
        {
            var state = await js.InvokeAsync<InstallState>("trackerInstall.init", _reference);
            Apply(state);
        }
        catch (JSException)
        {
            // Browser ohne Installationsunterstützung, dann bleibt der Streifen weg.
        }
    }

    [JSInvokable]
    public void OnInstallAvailable()
    {
        CanPrompt = true;
        Changed?.Invoke();
    }

    [JSInvokable]
    public void OnInstalled()
    {
        _installed = true;
        CanPrompt = false;
        Changed?.Invoke();
    }

    public async Task InstallAsync()
    {
        try
        {
            var outcome = await js.InvokeAsync<string>("trackerInstall.prompt");
            if (outcome != "accepted")
            {
                await DismissAsync();
                return;
            }

            _installed = true;
        }
        catch (JSException)
        {
            await DismissAsync();
        }

        CanPrompt = false;
        Changed?.Invoke();
    }

    public async Task DismissAsync()
    {
        _dismissed = true;
        await store.SetAsync(DismissedKey, "1");
        Changed?.Invoke();
    }

    private void Apply(InstallState state)
    {
        CanPrompt = state.CanPrompt;
        _standalone = state.IsStandalone;
        ShowIosHint = state.IsIos && !state.IsStandalone;
        Changed?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        _reference?.Dispose();
        _reference = null;
        return ValueTask.CompletedTask;
    }
}
