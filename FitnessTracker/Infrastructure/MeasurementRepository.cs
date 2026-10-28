using Azure;
using Azure.Data.Tables;
using FitnessTracker.Domain;

namespace FitnessTracker.Infrastructure;

internal sealed class MeasurementRepository(TableServiceClient service) : IMeasurementRepository
{
    private readonly TableClient _table = service.GetTableClient(TableNames.Measurements);

    public async Task<MeasurementDto?> GetAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var entity = await _table.GetEntityIfExistsAsync<MeasurementEntity>(
            TableNames.Owner,
            TableNames.RowKey(date),
            cancellationToken: cancellationToken
        );

        return entity.HasValue ? entity.Value!.ToDto() : null;
    }

    public async Task<IReadOnlyList<MeasurementDto>> ListAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken
    )
    {
        var filter = TableClient.CreateQueryFilter(
            $"PartitionKey eq {TableNames.Owner} and RowKey ge {TableNames.RowKey(from)} and RowKey le {TableNames.RowKey(to)}"
        );

        var measurements = new List<MeasurementDto>();
        var query = _table.QueryAsync<MeasurementEntity>(
            filter,
            cancellationToken: cancellationToken
        );

        await foreach (var entity in query)
        {
            measurements.Add(entity.ToDto());
        }

        return measurements;
    }

    public Task UpsertAsync(Measurement measurement, CancellationToken cancellationToken) =>
        _table.UpsertEntityAsync(
            MeasurementEntity.From(measurement),
            TableUpdateMode.Replace,
            cancellationToken
        );

    public async Task DeleteAsync(DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            await _table.DeleteEntityAsync(
                TableNames.Owner,
                TableNames.RowKey(date),
                ETag.All,
                cancellationToken
            );
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            // Deleting a day that was never written is the outcome the caller asked for.
        }
    }
}

internal sealed class ProfileRepository(TableServiceClient service) : IProfileRepository
{
    private readonly TableClient _table = service.GetTableClient(TableNames.Profile);

    public async Task<ProfileDto?> GetAsync(CancellationToken cancellationToken)
    {
        var entity = await _table.GetEntityIfExistsAsync<ProfileEntity>(
            TableNames.Owner,
            TableNames.ProfileRow,
            cancellationToken: cancellationToken
        );

        return entity.HasValue ? entity.Value!.ToDto() : null;
    }

    public Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken) =>
        _table.UpsertEntityAsync(
            ProfileEntity.From(profile),
            TableUpdateMode.Replace,
            cancellationToken
        );
}
