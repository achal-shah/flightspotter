# AircraftDataSync

Simple console app to update the Aircraft database.  We first have to download aircraft data from a site like: [OpenSky](https://opensky-network.org/datasets/#metadata/)

1. Download the csv file and save it locally.
2. Update Program.cs with the path to the file and the connection string of the table store.

Run locally (PowerShell):

```powershell
dotnet run
```

This will update any entries with missing data and add entries which were not there previously.

The `Aircraft` table has properties:

| Field         | Meaning |
|---------------|----------|
| PartitionKey  | Always set to 'Aircraft'  |
| RowKey        | The aircraft code  |
| IcaoAircraftType | The model of an aircraft |
| IcaoOperator     | The airline operating the aircraft |
