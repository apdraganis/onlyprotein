using OnlyProtein.Core.Models;

namespace OnlyProtein.Web.Services;

public sealed record RecentProduct(
    string Barcode,
    string ProductName,
    string? Brand,
    string? ImageUrl,
    decimal? Quantity,
    QuantityUnit Unit,
    decimal? Price,
    decimal? CaloriesPer100,
    decimal? ProteinPer100,
    decimal? EurPer100Kcal,
    decimal? EurPer100gProtein,
    DateTimeOffset SavedAt);
