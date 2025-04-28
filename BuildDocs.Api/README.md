BuildDocs.Api 
This project provides a starter template for an ASP.NET Core Web API with built-in support for:

-> Health checks (/healthz)

-> Swagger UI documentation (/swagger)

-> HTTPS support

-> Development and production environment configuration

        PREREQUISITES
Before you begin, ensure you have the following installed:
    -> At least .NET 6 SDK
    -> Any modern IDE (Visual Studio 2022, VS Code)

        HOW TO RUN LOCALLY
1. Clone the repository:

    git clone https://github.com/amosproj/amos2025ss02-building-documentation-management-system.git

    cd BuildDocs.Api

2. Trust the development HTTPS certificate (only needed once per machine):

    dotnet dev-certs https --trust

3. Restore dependencies:

    dotnet restore

4. Run the project:

    dotnet run

5. Access the API: 

    -> Swagger UI: https://localhost:5001/swagger
    -> Health check endpoint: https://localhost:5001/healthz


        NOTES:

-> The default ports are 5000 (HTTP) and 5001 (HTTPS). 

-> Please make sure no firewall or antivirus blocks localhost HTTPS.

-> To customize health checks, modify the services in Program.cs

