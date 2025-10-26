using System.Text.Json.Serialization;

namespace Hng_Stage2_BackendTrack.Dto
{
    public class CountryResponseDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("capital")]
        public string Capital { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("population")]
        public long Population { get; set; }

        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("exchange_rate")]
        public double? ExchangeRate { get; set; }

        [JsonPropertyName("estimated_gdp")]
        public double? EstimatedGdp { get; set; }

        [JsonPropertyName("flag_url")]
        public string? FlagUrl { get; set; }

        [JsonPropertyName("last_refreshed_at")]
        public DateTime LastRefreshedAt { get; set; }
    }

}
