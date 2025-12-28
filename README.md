# ArchoCybo

## Run (Local)
- Prerequisites: .NET 10 SDK, SQL Server LocalDB
- Start API: `dotnet run --project ArchoCybo.WebApi/ArchoCybo.WebApi.csproj`
- Start Frontend: `dotnet run --project ArchoCybo/ArchoCybo.csproj`
- Swagger: http://localhost:65110/swagger
- Blazor: http://localhost:5170
- Demo credentials: admin / ChangeMe123!

## Configuration
- WebApi connection string: appsettings.json key ConnectionStrings:DefaultConnection
- JWT settings: appsettings.json key Jwt (Key, Issuer)
- Blazor API base: appsettings.Development.json key ApiBaseUrl

## Docker (Backend + Frontend + SQL)
- Requirements: Docker Desktop
- Build and run:
  - `docker compose build`
  - `docker compose up -d`
- Services:
  - SQL Server: mcr.microsoft.com/mssql/server:2022-latest (port 1433)
  - WebApi: ArchoCybo.WebApi on http://localhost:8080
  - Frontend: Blazor Server on http://localhost:5170
- Environment:
  - WebApi: `ConnectionStrings__DefaultConnection=Server=sqlserver;Database=ArchoCyboDb;User Id=sa;Password=Your_strong_password123!;TrustServerCertificate=True`
  - WebApi: `Jwt__Key=very_secret_key_12345`, `Jwt__Issuer=ArchoCybo`
  - Frontend: `ApiBaseUrl=http://webapi:8080`

## Notes
- In Docker, Blazor connects to WebApi via service name `webapi`.
- CORS allows all origins for development.
- Swagger enums render as strings.
