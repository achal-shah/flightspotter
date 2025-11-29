using FlightSpotter.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlightSpotter.Web.Controllers
{
    public class FlightsController : Controller
    {
        private readonly FlightTableService _svc;

        public FlightsController(FlightTableService svc)
        {
            _svc = svc;
        }

        public async Task<IActionResult> Index(string sort = "asc")
        {
            sort = sort == "desc" ? "desc" : "asc";
            var flights = await _svc.GetFlightsAsync(sort);
            ViewBag.Sort = sort;
            return View(flights);
        }

        [HttpGet("/debug/entities")]
        public async Task<IActionResult> DebugEntities(int count = 5)
        {
            // Return first `count` raw entities (property name -> value) for debugging
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
    }
}
