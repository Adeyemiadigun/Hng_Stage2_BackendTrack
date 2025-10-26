using Hng_Stage2_BackendTrack.Models;
using Hng_Stage2_BackendTrack.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using Hng_Stage2_BackendTrack.Dto;
using static Hng_Stage2_BackendTrack.Services.ExternalApiServices;

namespace Hng_Stage2_BackendTrack.Services
{
    public class CountryService
    {
        private readonly AppDbContext _db;
        private readonly ExternalApiService _api;

        private static readonly string CacheDir = Path.Combine(Directory.GetCurrentDirectory(), "cache");
        private static readonly string CacheImagePath = Path.Combine(CacheDir, "summary.png");

        public CountryService(AppDbContext db, ExternalApiService api)
        {
            _db = db;
            _api = api;
        }

        public async Task RefreshCountriesAsync()
        {
            JsonElement[] countriesJson;
            JsonElement exchangeData;

            try
            {
                countriesJson = await _api.FetchCountriesAsync();
                exchangeData = await _api.FetchExchangeRatesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"External data source unavailable: {ex.Message}");
            }

            if (!exchangeData.TryGetProperty("rates", out var rates))
                throw new Exception("Exchange rate data malformed.");

            var now = DateTime.UtcNow;
            var rnd = new Random();

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var c in countriesJson)
                {
                    var name = c.GetProperty("name").GetString() ?? "";
                    var population = c.TryGetProperty("population", out var pop) ? pop.GetInt64() : 0;
                    var region = c.TryGetProperty("region", out var r) ? r.GetString() : null;
                    var capital = c.TryGetProperty("capital", out var cap) ? cap.GetString() : null;
                    var flag = c.TryGetProperty("flag", out var f) ? f.GetString() : null;

                    string? currencyCode = null;
                    double? exchangeRate = null;
                    double? gdp = null;

                    if (c.TryGetProperty("currencies", out var currencies)
                        && currencies.ValueKind == JsonValueKind.Array
                        && currencies.GetArrayLength() > 0)
                    {
                        currencyCode = currencies[0].TryGetProperty("code", out var cc)
                                       ? cc.GetString()
                                       : null;

                        if (!string.IsNullOrEmpty(currencyCode)
                            && rates.TryGetProperty(currencyCode, out var rateEl)
                            && rateEl.ValueKind == JsonValueKind.Number)
                        {
                            exchangeRate = rateEl.GetDouble();
                            var multiplier = rnd.Next(1000, 2000);
                            gdp = exchangeRate > 0 ? (population * multiplier) / exchangeRate : null;
                        }
                    }

                    var existing = await _db.Countries
                        .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());

                    if (existing != null)
                    {
                        existing.Region = region;
                        existing.Capital = capital;
                        existing.FlagUrl = flag;
                        existing.Population = population;
                        existing.CurrencyCode = currencyCode;
                        existing.ExchangeRate = exchangeRate;
                        existing.EstimatedGdp = gdp;
                        existing.LastRefreshedAt = now;
                    }
                    else
                    {
                        _db.Countries.Add(new Country
                        {
                            Name = name,
                            Capital = capital,
                            Region = region,
                            Population = population,
                            CurrencyCode = currencyCode,
                            ExchangeRate = exchangeRate,
                            EstimatedGdp = gdp,
                            FlagUrl = flag,
                            LastRefreshedAt = now
                        });
                    }
                }

                await _db.SaveChangesAsync();

                await GenerateSummaryImageAsync(now);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<CountryResponseDto>> GetAllAsync(string? region = null, string? currency = null, string? sort = null)
        {
            var query = _db.Countries.AsQueryable();

            if (!string.IsNullOrEmpty(region))
                query = query.Where(c => c.Region == region);

            if (!string.IsNullOrEmpty(currency))
                query = query.Where(c => c.CurrencyCode == currency);

            if (sort == "gdp_desc")
                query = query.OrderByDescending(c => c.EstimatedGdp);

            return await query
                .Select(c => new CountryResponseDto
                {
                    Name = c.Name,
                    Capital = c.Capital,
                    Region = c.Region,
                    Population = c.Population,
                    CurrencyCode = c.CurrencyCode,
                    ExchangeRate = c.ExchangeRate,
                    EstimatedGdp = c.EstimatedGdp,
                    FlagUrl = c.FlagUrl,
                    LastRefreshedAt = c.LastRefreshedAt
                })
                .ToListAsync();
        }

        public async Task<CountryResponseDto?> GetByNameAsync(string name)
        {
            var c = await _db.Countries.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
            if (c == null) return null;

            return new CountryResponseDto
            {
                Name = c.Name,
                Capital = c.Capital,
                Region = c.Region,
                Population = c.Population,
                CurrencyCode = c.CurrencyCode,
                ExchangeRate = c.ExchangeRate,
                EstimatedGdp = c.EstimatedGdp,
                FlagUrl = c.FlagUrl,
                LastRefreshedAt = c.LastRefreshedAt
            };
        }

        public async Task<bool> DeleteByNameAsync(string name)
        {
            var c = await _db.Countries.FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
            if (c == null) return false;
            _db.Countries.Remove(c);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(int total, DateTime? lastRefreshed)> GetStatusAsync()
        {
            var total = await _db.Countries.CountAsync();
            var lastRefreshed = await _db.Countries.MaxAsync(c => (DateTime?)c.LastRefreshedAt);
            return (total, lastRefreshed);
        }

        public async Task<byte[]?> GetSummaryImageAsync()
        {
            if (!File.Exists(CacheImagePath))
                return null;
            return await File.ReadAllBytesAsync(CacheImagePath);
        }

        private async Task GenerateSummaryImageAsync(DateTime timestamp)
        {
            Directory.CreateDirectory(CacheDir);

            var topCountries = await _db.Countries
                .Where(c => c.EstimatedGdp != null)
                .OrderByDescending(c => c.EstimatedGdp)
                .Take(5)
                .ToListAsync();

            var total = await _db.Countries.CountAsync();

            using var bmp = new Bitmap(700, 400);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);

            var fontTitle = new Font("Arial", 20, FontStyle.Bold);
            var fontText = new Font("Arial", 14);
            var brush = Brushes.Black;

            g.DrawString("Country Summary", fontTitle, brush, 20, 20);
            g.DrawString($"Total Countries: {total}", fontText, brush, 20, 80);

            int y = 120;
            g.DrawString("Top 5 Countries by GDP:", fontText, brush, 20, y);
            y += 30;

            foreach (var c in topCountries)
            {
                g.DrawString($"{c.Name}: {c.EstimatedGdp:N2}", fontText, brush, 40, y);
                y += 25;
            }

            g.DrawString($"Last Refresh: {timestamp:yyyy-MM-dd HH:mm:ss} UTC", fontText, brush, 20, y + 30);

            bmp.Save(CacheImagePath, ImageFormat.Png);
        }
    }
}

