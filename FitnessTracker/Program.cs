using FitnessTracker;
using FitnessTracker.Auth;
using FitnessTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddPinAuth(builder.Configuration, builder.Environment);
builder.Services.AddTableStorage(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddLoginRateLimiter();

var app = builder.Build();

app.UseExceptionHandler();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapTrackerEndpoints();
app.MapFallbackToFile("index.html").AllowAnonymous();

await app.EnsureTablesAsync();

app.Run();
