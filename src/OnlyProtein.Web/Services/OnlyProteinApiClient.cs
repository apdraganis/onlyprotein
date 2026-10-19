using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OnlyProtein.Core.Models;

namespace OnlyProtein.Web.Services;

public class OnlyProteinApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly HttpClient _httpClient;

    public OnlyProteinApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductInfo?> GetProductAsync(string barcode)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ProductInfo>(
                $"api/products/{Uri.EscapeDataString(barcode)}",
                JsonOptions);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Indices?> CalculateAsync(CalculateRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/calculate", request, JsonOptions);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<Indices>(JsonOptions);
    }
}
