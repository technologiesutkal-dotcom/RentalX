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
# Must match Railway "Generate Domain" port (8080). Do not use a different $PORT.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV PORT=8080
EXPOSE 8080

CMD ["dotnet", "RentalX.LicenseApi.dll"]

