# FlightSpotter.Web

Simple local ASP.NET Core MVC app that reads `Flights` from local Azurite table storage and displays a table with sorting by Time.

- Prerequisites
- .NET 10 SDK
- Azurite running locally (or another Table storage endpoint)

Default storage config in `appsettings.json` uses an Azurite-style connection string. Update `Storage:ConnectionString` if you use a different endpoint.

Run locally (PowerShell):

```powershell
dotnet restore
dotnet run --project .\FlightSpotter.Web\
```

Open http://localhost:5000/ in your browser. Click the Time header to toggle sort order.

Populating the `Flights` table
- Use Azure Storage Explorer or Azurite tools to create a table named `Flights` and insert entities with properties: `Country`, `Flight`, `Time`, `AircraftCode`, `Registration`, `AircraftType`, `Altitude`, `Heading`, `Latitude`, `Longitude`.

Notes
- The app falls back to empty list if Azurite isn't running.
- Sorting is performed in-memory by the `Time` property when parsable as `HH:mm:ss` or ISO datetime.
