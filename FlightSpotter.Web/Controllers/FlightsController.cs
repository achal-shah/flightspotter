using FlightSpotter.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace FlightSpotter.Web.Controllers
{
    public class FlightsController : Controller
    {
        private readonly FlightTableService _svc;

        public FlightsController(FlightTableService svc)
        {
            _svc = svc;
        }

        public async Task<IActionResult> Index(string sort = "desc")
        {
            sort = sort == "desc" ? "desc" : "asc";
            var flights = await _svc.GetFlightsAsync(sort);
            ViewBag.Sort = sort;
            return View(flights);
        }

        /// <summary>
        /// Return first `count` raw entities (property name -> value) for debugging
        /// </summary>
        /// <param name="count">Number of entities to return</param>
        /// <returns>The entities.</returns>
        [HttpGet("/debug/entities")]
        public async Task<IActionResult> DebugEntities(int count = 5)
        {
            try
            {
                var raw = await _svc.GetRawEntitiesAsync(count);
                return Json(new { count = raw.Count, entities = raw });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // New: route example -> GET /Flights/166244/0
        [HttpGet("/Flights/{rowKey}/{daysAgo?}")]
        public async Task<IActionResult> ByLocation(string rowKey, int daysAgo = 0, string sort = "desc")
        {
            if (string.IsNullOrEmpty(rowKey))
                return BadRequest(new { error = "rowKey is required" });

            try
            {
                // Read TimeZone for the rowKey from Locations table
                var timeZoneId = await _svc.GetLocationTimeZoneAsync(rowKey);
                TimeZoneInfo tz;
                try
                {
                    // Try to find system time zone; fall back to UTC on failure
                    tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId ?? "UTC");
                }
                catch
                {
                    tz = TimeZoneInfo.Utc;
                }

                // Current date in that time zone, then subtract daysAgo
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
                var targetDate = localNow.Date.AddDays(-daysAgo);

                // expose the timezone id to the view so times can be displayed in this tz
                ViewBag.TimeZoneId = tz.Id;

                // Partition format: {rowKey}_{YYYY}_{DayOfYear} (example: 166244_2025_332)
                var partitionKey = $"{rowKey}_{targetDate.Year}_{targetDate.DayOfYear}";

                // Fetch flights for the computed partition
                var flights = await _svc.GetFlightsByPartitionAsync(partitionKey);

                // normalize sort and apply ordering
                sort = sort == "desc" ? "desc" : "asc";
                if (sort == "desc")
                    flights = flights.OrderByDescending(f => f.TimeAsDateTime ?? DateTime.MinValue).ToList();
                else
                    flights = flights.OrderBy(f => f.TimeAsDateTime ?? DateTime.MaxValue).ToList();

                ViewBag.Partition = partitionKey;
                // expose the target date to the view so the header can show the selected day
                ViewBag.TargetDate = targetDate;
                // indicate current sort so the view can render the correct sort arrow
                ViewBag.Sort = sort;
                return View("Index", flights); // reuse Index view for listing
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
