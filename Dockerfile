FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/OnlyProtein.Core/OnlyProtein.Core.csproj src/OnlyProtein.Core/
COPY src/OnlyProtein.Api/OnlyProtein.Api.csproj src/OnlyProtein.Api/
COPY src/OnlyProtein.Web/OnlyProtein.Web.csproj src/OnlyProtein.Web/

RUN dotnet restore src/OnlyProtein.Api/OnlyProtein.Api.csproj

COPY src/ src/

RUN dotnet publish src/OnlyProtein.Api/OnlyProtein.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "OnlyProtein.Api.dll"]
