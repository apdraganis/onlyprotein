namespace OnlyProtein.Web.Services;

public sealed record TestBarcode(string Name, string Barcode);

public static class TestBarcodeCatalog
{
    public static IReadOnlyList<TestBarcode> Items { get; } =
    [
        new("Cheasy Skyr Naturel (1 kg)", "5760466996686"),
        new("Milbona Chocolate Protein Drink", "4335619140196"),
        new("Milbona High Protein Peanut & Chocolate Drink", "4335619038004"),
        new("Milbona High Protein Drink Banana", "4335619140202"),
    ];
}
