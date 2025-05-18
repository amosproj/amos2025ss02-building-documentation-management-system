# BUILD.ING

This project provides a starter template for an ASP.NET Core Web API with built-in support for:

-> Health checks (/healthz)

-> Swagger UI documentation (/swagger)

-> HTTPS support

-> Development and production environment configuration

## PREREQUISITES

Before you begin, ensure you have the following installed:

-> At least .NET 8 SDK

-> Any modern IDE (Visual Studio 2022, VS Code)

## HOW TO RUN LOCALLY

1. Clone the repository:
```bash
git clone https://github.com/amosproj/amos2025ss02-building-documentation-management-system.git
cd BitAndBeam/backend/BUILD.ING
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

---

## Data Model: Organization Entity

### Organization
The `Organization` entity represents a logical grouping for users and buildings. Every user and building must belong to exactly one organization. This enables multi-tenancy and strict data access boundaries between organizations.

**Fields:**
- `OrganizationId` (int, PK)
- `Name` (string, unique, required)
- `Description` (string, optional)
- `CreatedAt` (DateTime)
- `IsActive` (bool)

**Relationships:**
- **Users:** Each user is required to have an `OrganizationId` and only has access to data within their organization.
- **Buildings:** Each building is required to have an `OrganizationId` and belongs to one organization.

**Navigation properties:**
- `Organization.Users` — All users in the organization
- `Organization.Buildings` — All buildings owned by the organization

**Purpose:**
The addition of the Organization model enforces data segregation and access control, ensuring users can only interact with data belonging to their organization.

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

