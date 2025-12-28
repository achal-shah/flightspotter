// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
await AircraftSync.Sync(
    connectionString: "<connection string to Azure Table Storage>",
    tableName: "Aircraft",
    csvPath: "<path to aircraft database CSV file>"
);

