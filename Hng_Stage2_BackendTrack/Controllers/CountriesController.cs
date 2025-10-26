using Hng_Stage2_BackendTrack.Exceptions;
using Hng_Stage2_BackendTrack.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hng_Stage2_BackendTrack.Controllers
{
    [ApiController]
    [Route("countries")]
    public class CountriesController : ControllerBase
    {
        private readonly CountryService _countryService;

        public CountriesController(CountryService countryService)
        {
            _countryService = countryService;
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                await _countryService.RefreshCountriesAsync();
                return Ok(new { message = "Countries refreshed successfully" });
            }
            catch (ExternalApiException ex)
            {
                var apiName = ex.ApiName switch
                {
                    "restcountries" => "RESTCountries API",
                    "exchange_rates" => "Exchange Rate API",
                    _ => ex.ApiName
                };
                return StatusCode(503, new
                {
                    error = "External data source unavailable",
                    details = $"Could not fetch data from {apiName}"
                });
            }
            catch (Exception ex)
            {
                // 👇 Add this line for debugging only
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    details = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? region, [FromQuery] string? currency, [FromQuery] string? sort)
        {
            var result = await _countryService.GetAllAsync(region, currency, sort);
            return Ok(result);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetByName(string name)
        {
            var country = await _countryService.GetByNameAsync(name);
            if (country == null)
                return NotFound(new { error = "Country not found" });
            return Ok(country);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            var deleted = await _countryService.DeleteByNameAsync(name);
            if (!deleted)
                return NotFound(new { error = "Country not found" });
            return NoContent();
        }

        [HttpGet("/status")]
        public async Task<IActionResult> GetStatus()
        {
            var (total, lastRefreshed) = await _countryService.GetStatusAsync();
            return Ok(new
            {
                total_countries = total,
                last_refreshed_at = lastRefreshed
            });
        }

        [HttpGet("image")]
        public async Task<IActionResult> GetImage()
        {
            var imageBytes = await _countryService.GetSummaryImageAsync();
            if (imageBytes == null)
                return NotFound(new { error = "Summary image not found" });
            return File(imageBytes, "image/png");
        }
    }
}
