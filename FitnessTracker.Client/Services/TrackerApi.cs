using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace FitnessTracker.Client.Services;

/// <summary>A rejected value from the API, carrying the sentence the app shows.</summary>
public sealed class ApiException(string message) : Exception(message);

public sealed class TrackerApi(HttpClient http, AuthStore auth, NavigationManager navigation)
{
    public async Task<bool> LoginAsync(string pin)
    {
        var response = await http.PostAsJsonAsync("auth/login", new LoginRequest(pin));
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (login is null)
        {
            return false;
        }

        await auth.SetTokenAsync(login.Token);
        return true;
    }

    public async Task SignOutAsync()
    {
        await auth.ClearAsync();
        navigation.NavigateTo("anmelden", replace: true);
    }

    public Task<MeasurementDto?> GetMeasurementAsync(DateOnly date) =>
        SendAsync<MeasurementDto?>(HttpMethod.Get, $"api/measurements/{Key(date)}", null, true);

    public async Task<IReadOnlyList<MeasurementDto>> ListMeasurementsAsync(
        DateOnly from,
        DateOnly to
    ) =>
        await SendAsync<List<MeasurementDto>>(
            HttpMethod.Get,
            $"api/measurements?from={Key(from)}&to={Key(to)}",
            null,
            false
        ) ?? [];

    public Task<MeasurementDto?> SaveMeasurementAsync(DateOnly date, MeasurementInput input) =>
        SendAsync<MeasurementDto?>(HttpMethod.Put, $"api/measurements/{Key(date)}", input, false);

    public Task DeleteMeasurementAsync(DateOnly date) =>
        SendAsync<object?>(HttpMethod.Delete, $"api/measurements/{Key(date)}", null, false);

    public async Task<ProfileDto> GetProfileAsync() =>
        await SendAsync<ProfileDto>(HttpMethod.Get, "api/profile", null, false) ?? new ProfileDto();

    public async Task<ProfileDto> SaveProfileAsync(ProfileDto profile) =>
        await SendAsync<ProfileDto>(HttpMethod.Put, "api/profile", profile, false)
        ?? new ProfileDto();

    private static string Key(DateOnly date) => date.ToString("yyyy-MM-dd");

    private async Task<T?> SendAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        bool nullOnNotFound
    )
    {
        using var request = new HttpRequestMessage(method, url);

        var token = await auth.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType());
        }

        using var response = await http.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await SignOutAsync();
            throw new ApiException("Die Anmeldung ist abgelaufen.");
        }

        if (nullOnNotFound && response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadProblemAsync(response));
        }

        if (
            response.StatusCode == HttpStatusCode.NoContent
            || response.Content.Headers.ContentLength is null or 0
        )
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private static async Task<string> ReadProblemAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemTitle>();
            if (!string.IsNullOrWhiteSpace(problem?.Title))
            {
                return problem.Title;
            }
        }
        catch (Exception)
        {
            // Fall through to the generic sentence below.
        }

        return "Das Speichern hat nicht geklappt. Bitte später nochmals versuchen.";
    }

    private sealed record ProblemTitle(string? Title);
}
