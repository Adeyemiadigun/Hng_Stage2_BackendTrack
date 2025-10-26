using System.Text.Json;

namespace Hng_Stage2_BackendTrack.Services
{
    public class ExternalApiServices
    {
        public class ExternalApiService
        {
            private readonly HttpClient _httpClient;

            public ExternalApiService(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public async Task<JsonElement[]> FetchCountriesAsync()
            {
                var url = "https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies";
                return await _httpClient.GetFromJsonAsync<JsonElement[]>(url)
                       ?? throw new Exception("Failed to fetch countries.");
            }

            public async Task<JsonElement> FetchExchangeRatesAsync()
            {
                var url = "https://open.er-api.com/v6/latest/USD";
                return await _httpClient.GetFromJsonAsync<JsonElement>(url);
            }
        }
    }
}
