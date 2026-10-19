using System.Text.Json;
using Microsoft.JSInterop;

namespace OnlyProtein.Web.Services;

public sealed class RecentProductsService
{
    private const string StorageKey = "onlyprotein.recent-products";
    private const int MaxItems = 10;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IJSRuntime _jsRuntime;

    public RecentProductsService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<IReadOnlyList<RecentProduct>> GetAsync()
    {
        var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<RecentProduct>>(json, JsonOptions)
                ?? throw new InvalidOperationException("Recent products storage contained a null value.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Recent products storage contains invalid data.", ex);
        }
    }

    public async Task AddAsync(RecentProduct product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var products = (await GetAsync())
            .Where(existing => !string.Equals(existing.Barcode, product.Barcode, StringComparison.Ordinal))
            .Prepend(product)
            .Take(MaxItems)
            .ToList();

        await SaveAsync(products);
    }

    public Task ClearAsync() =>
        _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey).AsTask();

    private Task SaveAsync(IReadOnlyList<RecentProduct> products)
    {
        var json = JsonSerializer.Serialize(products, JsonOptions);
        return _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json).AsTask();
    }
}
