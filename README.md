# OnlyProtein

Point the camera at a barcode (or type it in), enter what you paid, and get two indices in EUR:

- cost per 100 kcal
- cost per 100g protein

Product nutrition comes from [Open Food Facts](https://world.openfoodfacts.org/). Solids use **per 100g** values; liquids use **per 100ml** when the pack is in ml/L. When data is missing, you can fill in kcal and protein manually.

## Architecture

Single-repo .NET 10 monolith with three projects:

| Project | Role |
|---------|------|
| `OnlyProtein.Core` | Shared models and index math |
| `OnlyProtein.Api` | Minimal API — Open Food Facts proxy, calculate endpoint, hosts the WASM app in production |
| `OnlyProtein.Web` | Blazor WebAssembly PWA with camera barcode scanning |

In production, the API serves both `/api/*` and the static Blazor app from one origin (no CORS). Local development runs API and web on separate ports with CORS enabled.

```
Browser (PWA)
    │  barcode scan + price input
    ▼
OnlyProtein.Api
    │  product lookup
    ▼
Open Food Facts API
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Run locally

### Visual Studio

Select the checked-in `Api-Web` multi-project launch profile. It starts both
`OnlyProtein.Api` and `OnlyProtein.Web`; only the web project opens a browser,
and the API runs in the background on `http://localhost:5234`.

If the profile is not available in an older Visual Studio version, right-click
the solution, choose **Properties**, open **Common Properties > Startup
Project**, select **Multiple startup projects**, then set both projects to
**Start**. Keep both projects on their `http` profile.

### Command line

Start the API:

```bash
dotnet run --project src/OnlyProtein.Api
```

In another terminal, start the web app:

```bash
dotnet run --project src/OnlyProtein.Web
```

Open `http://localhost:5210`. The web app calls the API at `http://localhost:5234` (see `src/OnlyProtein.Web/wwwroot/appsettings.Development.json`).



## Tests

```bash
dotnet test
```

## Docker

Build and run the combined API + PWA image:

```bash
docker build -t onlyprotein .
docker run -p 8080:8080 onlyprotein
```

Open `http://localhost:8080`.

## Test barcodes

The barcode form includes quick-fill examples for products currently verified in the Danish Open Food Facts catalog:

| Product | Barcode |
|---------|---------|
| Cheasy Skyr Naturel (1 kg) | `5760466996686` |
| Milbona Chocolate Protein Drink | `4335619140196` |
| Milbona High Protein Peanut & Chocolate Drink | `4335619038004` |
| Milbona High Protein Drink Banana | `4335619140202` |

The exact Murph protein powder barcode could not be verified from publicly indexed Bilka/Open Food Facts data, so it is intentionally not included as a guessed value.

## Camera & PWA

Barcode scanning uses the device camera via `getUserMedia`. Browsers only allow that on **secure contexts** — `https://` or `http://localhost`. If you deploy without HTTPS, the scanner will fail and manual entry still works.

The app is installable as a PWA (`manifest.json`, service worker). Icons and offline shell are included; product lookup still needs network access.
