using System.Text.Json;
using Hng_Stage2_BackendTrack.Exceptions;

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
                try
                {
                    var result = await _httpClient.GetFromJsonAsync<JsonElement[]>(url);
                    if (result == null)
                        throw new ExternalApiException("restcountries", "Received null response.");

                    return result;
                }
                catch (HttpRequestException ex)
                {
                    throw new ExternalApiException("restcountries", $"HTTP error: {ex.Message}");
                }
                catch (NotSupportedException ex)
                {
                    throw new ExternalApiException("restcountries", "Invalid content type returned.");
                }
                catch (JsonException ex)
                {
                    throw new ExternalApiException("restcountries", "Malformed JSON from API.");
                }
            }


            public async Task<JsonElement> FetchExchangeRatesAsync()
            {
                var url = "https://open.er-api.com/v6/latest/USD";
                try
                {
                    var result = await _httpClient.GetFromJsonAsync<JsonElement>(url);
                    if (result.ValueKind == JsonValueKind.Undefined)
                        throw new ExternalApiException("exchange_rates", "Received undefined JSON value.");

                    return result;
                }
                catch (HttpRequestException ex)
                {
                    throw new ExternalApiException("exchange_rates", $"HTTP error: {ex.Message}");
                }
                catch (NotSupportedException ex)
                {
                    throw new ExternalApiException("exchange_rates", "Invalid content type returned.");
                }
                catch (JsonException ex)
                {
                    throw new ExternalApiException("exchange_rates", "Malformed JSON from API.");
                }
            }

        }
    }
}
