using Azure.Data.Tables;
using Azure.Identity;

namespace FitnessTracker.Infrastructure;

public static class TableStorageSetup
{
    /// <summary>
    /// Local development uses a connection string (Azurite). In Azure the container app's
    /// managed identity talks to the table endpoint, so no secret is involved.
    /// </summary>
    public static IServiceCollection AddTableStorage(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration["Storage:ConnectionString"];
        var tableEndpoint = configuration["Storage:TableEndpoint"];

        if (string.IsNullOrWhiteSpace(connectionString) && string.IsNullOrWhiteSpace(tableEndpoint))
        {
            throw new InvalidOperationException(
                "Storage:ConnectionString or Storage:TableEndpoint must be configured."
            );
        }

        services.AddSingleton(_ =>
            string.IsNullOrWhiteSpace(connectionString)
                ? new TableServiceClient(new Uri(tableEndpoint!), new DefaultAzureCredential())
                : new TableServiceClient(connectionString)
        );

        services.AddSingleton<IMeasurementRepository, MeasurementRepository>();
        services.AddSingleton<IProfileRepository, ProfileRepository>();

        return services;
    }

    /// <summary>
    /// Creates both tables if they are missing. Runs before the first request because a
    /// query against a missing table fails, and two creates cost well under a second.
    /// </summary>
    public static async Task EnsureTablesAsync(this WebApplication app)
    {
        var client = app.Services.GetRequiredService<TableServiceClient>();

        try
        {
            await client.CreateTableIfNotExistsAsync(TableNames.Measurements);
            await client.CreateTableIfNotExistsAsync(TableNames.Profile);
        }
        catch (Exception exception)
        {
            app.Logger.LogError(
                exception,
                "Could not create the storage tables. The app starts anyway and retries on the next deployment."
            );
        }
    }
}
