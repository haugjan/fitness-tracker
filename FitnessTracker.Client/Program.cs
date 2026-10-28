using FitnessTracker.Client;
using FitnessTracker.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});
builder.Services.AddScoped<LocalStore>();
builder.Services.AddScoped<AuthStore>();
builder.Services.AddScoped<TrackerApi>();
builder.Services.AddScoped<PwaUpdateService>();
builder.Services.AddScoped<InstallPrompt>();

await builder.Build().RunAsync();
