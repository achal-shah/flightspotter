using System.Globalization;

namespace FlightSpotter.Web.Models
{
    public class FlightEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;
        public string Flight { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty; // stored as HH:mm:ss or ISO
        public string AircraftCode { get; set; } = string.Empty;
        public string Registration { get; set; } = string.Empty;
        public string AircraftType { get; set; } = string.Empty;
        public string Altitude { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string Latitude { get; set; } = string.Empty;
        public string Longitude { get; set; } = string.Empty;

        public DateTime? TimeAsDateTime
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Time)) return null;
                // Try parse as HH:mm:ss
                if (TimeSpan.TryParse(Time, out var ts))
                {
                    // return today with that time
                    return DateTime.Today.Add(ts);
                }
                if (DateTime.TryParse(Time, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                {
                    return dt;
                }
                return null;
            }
        }
    }
}
