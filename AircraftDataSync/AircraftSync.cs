using Azure;
using Azure.Data.Tables;
using System;
using System.IO;
using System.Linq;

public class AircraftEntity : ITableEntity
{
    public string? PartitionKey { get; set; }
    public string? RowKey { get; set; }
    public string? IcaoAircraftType { get; set; }
    public string? IcaoOperator { get; set; }
    public string? Registration { get; set; }

    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}

public static class AircraftSync
{
    public static async Task Sync(string connectionString, string tableName, string csvPath)
    {
        var service = new TableServiceClient(connectionString);
        var table = service.GetTableClient(tableName);

        var lines = File.ReadAllLines(csvPath);
        // Strip single quotes from the header line 
        var headerLine = lines[0].Replace("'", "");
        var headers = headerLine.Split(',');

        int idxIcao24 = Array.IndexOf(headers, "icao24");
        int idxType = Array.IndexOf(headers, "typecode");
        int idxOpIcao = Array.IndexOf(headers, "operatorIcao");
        int idxReg = Array.IndexOf(headers, "registration");

        int insertCount = 0;
        int updatedCount = 0;

        await Parallel.ForEachAsync(lines.Skip(1), async (rawLine, token) =>
        {
            var cleanedLine = rawLine.Replace("'", "");
            var cols = cleanedLine.Split(',');

            // Skip if required fields missing
            try
            {
                if (string.IsNullOrWhiteSpace(cols[idxIcao24]) ||
                    string.IsNullOrWhiteSpace(cols[idxType]) ||
                    string.IsNullOrWhiteSpace(cols[idxReg]) ||
                    string.Equals(cols[idxReg], "-unknown-", StringComparison.OrdinalIgnoreCase))
                    return;
            }
            catch (IndexOutOfRangeException)
            {
                // Malformed line
                return;
            }

            string rowKey = (cols[idxIcao24]).ToUpperInvariant();

            AircraftEntity entity = new()
            {
                PartitionKey = "Aircraft",
                RowKey = rowKey,
                IcaoAircraftType = cols[idxType],
                IcaoOperator = cols[idxOpIcao],
                Registration = cols[idxReg]
            };

            try
            {
                AircraftEntity existing = await table.GetEntityAsync<AircraftEntity>("Aircraft", rowKey);

                bool updated = false;

                if (string.IsNullOrWhiteSpace(existing.IcaoAircraftType))
                {
                    existing.IcaoAircraftType = entity.IcaoAircraftType;
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.IcaoOperator))
                {
                    existing.IcaoOperator = entity.IcaoOperator;
                    updated = true;
                }
                if (string.IsNullOrWhiteSpace(existing.Registration))
                {
                    existing.Registration = entity.Registration;
                    updated = true;
                }

                if (updated)
                {
                    await table.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Merge, token);
                    Console.WriteLine($"{++updatedCount} Updated {rowKey}");
                }
            }
            catch (RequestFailedException)
            {
                // Entity does not exist → create it
                await table.AddEntityAsync(entity, token);
                Console.WriteLine($"{++insertCount} Inserted {rowKey}");
            }
        });

        Console.WriteLine("Sync complete.");
    }
}
