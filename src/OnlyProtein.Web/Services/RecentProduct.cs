namespace OnlyProtein.Web.Services;

public sealed record RecentProduct(
    string Barcode,
    string ProductName,
    string? Brand,
    decimal? Quantity,
    decimal? CaloriesPer100,
    decimal? ProteinPer100,
    DateTimeOffset SavedAt);
