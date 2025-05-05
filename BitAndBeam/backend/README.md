# BUILD.ING

This project provides a starter template for an ASP.NET Core Web API with built-in support for:

-> Health checks (/healthz)

-> Swagger UI documentation (/swagger)

-> HTTPS support

-> Development and production environment configuration

## PREREQUISITES

Before you begin, ensure you have the following installed:

-> At least .NET 6 SDK

-> Any modern IDE (Visual Studio 2022, VS Code)

## HOW TO RUN LOCALLY

1. Clone the repository:
```bash
git clone https://github.com/amosproj/amos2025ss02-building-documentation-management-system.git
cd backend/BUILD.ING
```

2. Trust the development HTTPS certificate (only needed once per machine):
```bash
dotnet dev-certs https --trust
```

3. Restore dependencies:
```bash
dotnet restore
```

4. Run the project:
```bash
dotnet run
```

5. Access the API: 

-> Swagger UI: https://localhost:5001/swagger

-> Health check endpoint: https://localhost:5001/healthz

## Code formatting

```bash
## Formatting Code

#To check formatting compliance:
dotnet format --verify-no-changes

#To fix formatting:
dotnet format
```


## NOTES:

-> The default ports are 5000 (HTTP) and 5001 (HTTPS). 

-> Please make sure no firewall or antivirus blocks localhost HTTPS.

-> To customize health checks, modify the services in Program.cs

