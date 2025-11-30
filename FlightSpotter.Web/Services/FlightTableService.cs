using Azure;
using Azure.Data.Tables;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlightSpotter.Web.Models;
using Microsoft.Extensions.Configuration;

namespace FlightSpotter.Web.Services
{
    public class FlightTableService
    {
        private readonly TableClient _flightsTableClient;
        private readonly TableClient _locationsTableClient;

        public FlightTableService(IConfiguration config)
        {
            var conn = config.GetSection("Storage").GetValue<string>("ConnectionString") ?? "UseDevelopmentStorage=true";
            var flightsTableName = config.GetSection("Storage").GetValue<string>("TableName") ?? "Flights";
            var locationsTableName = config.GetSection("Storage").GetValue<string>("LocationsTable") ?? "Locations";

            // Create clients
            _flightsTableClient = new TableClient(conn, flightsTableName);
            _locationsTableClient = new TableClient(conn, locationsTableName);
            try
            {
                _flightsTableClient.CreateIfNotExists();
            }
            catch
            {
                throw;
            }
            try
            {
                _locationsTableClient.CreateIfNotExists();
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<FlightEntity>> GetFlightsAsync(string sortOrder = "asc")
        {
            var results = new List<FlightEntity>();

            try
            {
                await foreach (var e in _flightsTableClient.QueryAsync<TableEntity>())
                {
                    var f = new FlightEntity
                    {
                        PartitionKey = e.PartitionKey,
                        RowKey = e.RowKey,
                        // try common alternative property names used by different data sources
                        Country = e.GetFirstStringOrDefault("Country", "OriginCountry", "origin_country"),
                        Flight = e.GetFirstStringOrDefault("Flight", "Callsign", "CallSign", "callsign", "flight").Trim(),
                        Time = e.GetFirstStringOrDefault("Time", "Seen", "SeenTime", "TimeUTC", "TimeUtc", "timestamp", "PositionTimestampUtc", "Timestamp"),
                        AircraftCode = e.GetFirstStringOrDefault("AircraftCode", "Aircraft", "Icao24", "ICAO", "Icao", "Hex", "ModeS", "ModeSCode"),
                        Registration = e.GetFirstStringOrDefault("Registration", "Reg", "registration"),
                        AircraftType = e.GetFirstStringOrDefault("AircraftType", "Type", "aircraft_type"),
                        Altitude = e.GetFirstStringOrDefault("Altitude", "Alt", "altitude"),
                        Heading = e.GetFirstStringOrDefault("Heading", "Course", "heading"),
                        Latitude = e.GetFirstStringOrDefault("Latitude", "Lat", "latitude"),
                        Longitude = e.GetFirstStringOrDefault("Longitude", "Lon", "Lng", "longitude")
                    };
                    // If Flight wasn't present in properties, use RowKey which often contains the callsign/flight id
                    if (string.IsNullOrWhiteSpace(f.Flight)) f.Flight = e.RowKey;

                    results.Add(f);
                }
            }
            catch
            {
                // return empty if table not available
            }

            // sort by TimeAsDateTime when available, otherwise by Time string
            var ordered = results.OrderBy(f => f.TimeAsDateTime ?? DateTime.MaxValue);
            if (sortOrder == "desc") ordered = results.OrderByDescending(f => f.TimeAsDateTime ?? DateTime.MinValue);

            return ordered.ToList();
        }

        // Expose raw entity retrieval from the underlying TableClient for debug endpoints
        public Task<List<Dictionary<string, object?>>> GetRawEntitiesAsync(int max = 5)
        {
            return _flightsTableClient.GetRawEntitiesAsync(max);
        }

        // Reads the TimeZone property from the Locations table for the given RowKey.
        public async Task<string?> GetLocationTimeZoneAsync(string rowKey)
        {
            if (string.IsNullOrEmpty(rowKey))
                return null;
            try
            {
                var resp = await _locationsTableClient.GetEntityAsync<TableEntity>("Location", rowKey);
                var entity = resp.Value;
                if (entity != null && entity.TryGetValue("TimeZone", out var tzObj))
                    return tzObj?.ToString();
                return null;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Not found
                return null;
            }
        }

        // Queries the Flights table for all entities whose PartitionKey equals the computed partitionKey.
        // Returns a list of FlightEntity so views can consume the expected model type.
        public async Task<List<FlightEntity>> GetFlightsByPartitionAsync(string partitionKey)
        {
            var results = new List<FlightEntity>();
            if (string.IsNullOrEmpty(partitionKey))
                return results;

            // escape single quotes in partitionKey
            var escaped = partitionKey.Replace("'", "''");
            var filter = $"PartitionKey eq '{escaped}'";

            await foreach (var e in _flightsTableClient.QueryAsync<TableEntity>(filter))
            {
                var f = new FlightEntity
                {
                    PartitionKey = e.PartitionKey,
                    RowKey = e.RowKey,
                    Country = e.GetFirstStringOrDefault("Country", "OriginCountry", "origin_country"),
                    Flight = e.GetFirstStringOrDefault("Flight", "Callsign", "CallSign", "callsign", "flight").Trim(),
                    Time = e.GetFirstStringOrDefault("Time", "Seen", "SeenTime", "TimeUTC", "TimeUtc", "timestamp", "PositionTimestampUtc", "Timestamp"),
                    AircraftCode = e.GetFirstStringOrDefault("AircraftCode", "Aircraft", "Icao24", "ICAO", "Icao", "Hex", "ModeS", "ModeSCode"),
                    Registration = e.GetFirstStringOrDefault("Registration", "Reg", "registration"),
                    AircraftType = e.GetFirstStringOrDefault("AircraftType", "Type", "aircraft_type"),
                    Altitude = e.GetFirstStringOrDefault("Altitude", "Alt", "altitude"),
                    Heading = e.GetFirstStringOrDefault("Heading", "Course", "heading"),
                    Latitude = e.GetFirstStringOrDefault("Latitude", "Lat", "latitude"),
                    Longitude = e.GetFirstStringOrDefault("Longitude", "Lon", "Lng", "longitude")
                };
                if (string.IsNullOrWhiteSpace(f.Flight)) f.Flight = e.RowKey;
                results.Add(f);
            }

            return results;
        }
    }

    internal static class TableEntityExtensions
    {
        public static string GetStringOrDefault(this TableEntity e, string prop)
        {
            if (e == null) return string.Empty;
            if (e.TryGetValue(prop, out var v) && v != null) return v.ToString() ?? string.Empty;
            return string.Empty;
        }

        public static string GetFirstStringOrDefault(this TableEntity e, params string[] props)
        {
            if (e == null) return string.Empty;
            if (props == null || props.Length == 0) return string.Empty;
            foreach (var p in props)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                if (e.TryGetValue(p, out var v) && v != null)
                {
                    var s = v.ToString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
            }
            return string.Empty;
        }

        public static async Task<List<Dictionary<string, object?>>> GetRawEntitiesAsync(this TableClient client, int max = 5)
        {
            var list = new List<Dictionary<string, object?>>();
            if (client == null) return list;
            try
            {
                await foreach (var e in client.QueryAsync<TableEntity>())
                {
                    var dict = new Dictionary<string, object?>();
                    // include partition/row keys explicitly
                    dict["PartitionKey"] = e.PartitionKey;
                    dict["RowKey"] = e.RowKey;
                    foreach (var kv in e)
                    {
                        dict[kv.Key] = kv.Value;
                    }
                    list.Add(dict);
                    if (list.Count >= max) break;
                }
            }
            catch
            {
                // ignore errors in debug retrieval
            }
            return list;
        }
    }
}
