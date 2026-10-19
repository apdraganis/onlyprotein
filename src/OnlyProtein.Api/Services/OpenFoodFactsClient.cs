using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnlyProtein.Core.Models;
using OnlyProtein.Core.Services;

namespace OnlyProtein.Api.Services;

public interface IOpenFoodFactsClient
{
    Task<ProductInfo?> GetProductAsync(string barcode, CancellationToken cancellationToken = default);
}

public class OpenFoodFactsClient : IOpenFoodFactsClient
{
    private const int MaxTransientRetries = 3;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsClient> _logger;

    public OpenFoodFactsClient(HttpClient httpClient, ILogger<OpenFoodFactsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductInfo?> GetProductAsync(string barcode, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= MaxTransientRetries; attempt++)
        {
            using var response = await _httpClient.GetAsync($"api/v2/product/{barcode}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Product not found in Open Food Facts. Barcode: {Barcode}", barcode);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (IsTransient(response.StatusCode) && attempt < MaxTransientRetries)
                {
                    var delay = GetRetryDelay(response, attempt);
                    _logger.LogWarning(
                        "Transient Open Food Facts response {StatusCode} for barcode {Barcode}; retrying in {DelayMilliseconds} ms.",
                        (int)response.StatusCode,
                        barcode,
                        delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                _logger.LogWarning(
                    "Open Food Facts returned {StatusCode} for barcode {Barcode} after {Attempts} attempt(s).",
                    (int)response.StatusCode,
                    barcode,
                    attempt + 1);

                response.EnsureSuccessStatusCode();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<OffProductResponse>(stream, JsonOptions, cancellationToken);

            if (payload?.Status != 1 || payload.Product is null)
            {
                _logger.LogWarning(
                    "Open Food Facts returned an invalid or incomplete product response for barcode {Barcode}.",
                    barcode);
                return null;
            }

            var product = payload.Product;
            var parsedQuantity = QuantityParser.Parse(product.Quantity);

            return new ProductInfo(
                Barcode: barcode,
                Name: product.ProductName ?? product.GenericName ?? "Unknown product",
                ImageUrl: product.ImageUrl,
                DefaultQuantity: parsedQuantity?.Value,
                DefaultQuantityUnit: parsedQuantity?.Unit,
                NutritionPer100g: TryGetNutrition(
                    product.Nutriments?.EnergyKcal100g,
                    product.Nutriments?.EnergyKj100g,
                    product.Nutriments?.Proteins100g),
                NutritionPer100ml: TryGetNutrition(
                    product.Nutriments?.EnergyKcal100ml,
                    product.Nutriments?.EnergyKj100ml,
                    product.Nutriments?.Proteins100ml));
        }

        return null;
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout ||
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is { } retryAfter)
            return retryAfter > TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : retryAfter;

        return TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt));
    }

    private static NutritionFacts? TryGetNutrition(decimal? kcal, decimal? energyKj, decimal? protein)
    {
        if (kcal is null or <= 0 && energyKj is > 0)
            kcal = energyKj / 4.184m;

        if (kcal is null or <= 0 || protein is null or <= 0)
            return null;

        return new NutritionFacts(kcal.Value, protein.Value);
    }

    private sealed class OffProductResponse
    {
        public int Status { get; init; }
        public OffProduct? Product { get; init; }
    }

    private sealed class OffProduct
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; init; }

        [JsonPropertyName("generic_name")]
        public string? GenericName { get; init; }

        [JsonPropertyName("quantity")]
        public string? Quantity { get; init; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; init; }

        public OffNutriments? Nutriments { get; init; }
    }

    private sealed class OffNutriments
    {
        [JsonPropertyName("energy-kcal_100g")]
        public decimal? EnergyKcal100g { get; init; }

        [JsonPropertyName("energy-kj_100g")]
        public decimal? EnergyKj100g { get; init; }

        [JsonPropertyName("proteins_100g")]
        public decimal? Proteins100g { get; init; }

        [JsonPropertyName("energy-kcal_100ml")]
        public decimal? EnergyKcal100ml { get; init; }

        [JsonPropertyName("energy-kj_100ml")]
        public decimal? EnergyKj100ml { get; init; }

        [JsonPropertyName("proteins_100ml")]
        public decimal? Proteins100ml { get; init; }
    }
}
