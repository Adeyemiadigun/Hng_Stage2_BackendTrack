using System.ComponentModel.DataAnnotations;

namespace Hng_Stage2_BackendTrack.Models
{
    public class Country
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Capital { get; set; }
        public string? Region { get; set; }

        [Required]
        public long Population { get; set; }

        public string? CurrencyCode { get; set; }
        public double? ExchangeRate { get; set; }
        public double? EstimatedGdp { get; set; }

        public string? FlagUrl { get; set; }
        public DateTime LastRefreshedAt { get; set; }
    }
}
