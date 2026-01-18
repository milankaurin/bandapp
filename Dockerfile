# -------- build stage --------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy csproj first (for cache)
COPY Band.Shared/Band.Shared.csproj Band.Shared/
COPY BandApplicationBack/BandApplicationBack.csproj BandApplicationBack/

RUN dotnet restore BandApplicationBack/BandApplicationBack.csproj

# copy everything
COPY . .

RUN dotnet publish BandApplicationBack/BandApplicationBack.csproj -c Release -o /app/publish

# -------- runtime stage --------
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BandApplicationBack.dll"]
