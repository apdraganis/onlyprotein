using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OnlyProtein.Web;
using OnlyProtein.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var baseAddress = string.IsNullOrWhiteSpace(apiBaseUrl)
    ? builder.HostEnvironment.BaseAddress
    : apiBaseUrl;

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(baseAddress, UriKind.Absolute)
});
builder.Services.AddScoped<OnlyProteinApiClient>();
builder.Services.AddScoped<BarcodeScannerService>();
builder.Services.AddScoped<RecentProductsService>();

var host = builder.Build();
Console.WriteLine($"[OnlyProtein] WASM started · build {AppBuildInfo.BuildId}");
await host.RunAsync();
