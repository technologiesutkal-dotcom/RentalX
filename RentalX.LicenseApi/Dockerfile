# Build from repo root:
#   docker build -f RentalX.LicenseApi/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RentalX.LicenseApi/RentalX.LicenseApi.csproj RentalX.LicenseApi/
RUN dotnet restore RentalX.LicenseApi/RentalX.LicenseApi.csproj

COPY RentalX.LicenseApi/ RentalX.LicenseApi/
WORKDIR /src/RentalX.LicenseApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV PORT=8080
EXPOSE 8080

# Prefer Railway PORT when set; otherwise 8080 (matches Generate Domain port).
CMD ["sh", "-c", "export ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}; echo Listening on $ASPNETCORE_URLS; exec dotnet RentalX.LicenseApi.dll"]
