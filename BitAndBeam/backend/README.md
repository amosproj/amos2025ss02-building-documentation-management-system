<div align="center">
  <h1>BitAndBeam Backend</h1>
  <p>Intelligent Document Management System - ASP.NET Core Backend</p>
</div>

![C#](https://img.shields.io/badge/C%23-8.0-brightgreen.svg)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue.svg)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-orange.svg)

## Overview

The BitAndBeam backend is a robust ASP.NET Core Web API that provides the server-side functionality for the document management system. It supports multi-tenant architecture, document processing, OCR, and AI-based classification, with the following key features:

- **RESTful API endpoints** for document and building management
- **Integrated health checks** (/healthz) for monitoring service status
- **Comprehensive API documentation** via Swagger UI (/swagger)
- **Secure JWT authentication** for protected resources
- **Multi-tenant data model** with strict organization boundaries
- **Document processing pipelines** integrated with Apache Tika and Ollama AI
- **Database migrations** for schema versioning and deployment

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8 SDK** or newer - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Modern IDE** - [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [VS Code](https://code.visualstudio.com/) with C# extension
- **Docker** (optional, for containerized development) - [Download](https://www.docker.com/products/docker-desktop)
- **PostgreSQL** (optional, for local development without Docker) - [Download](https://www.postgresql.org/download/)

You'll also need access to the following services if running the full stack:
- Apache Tika for OCR and document extraction
- Ollama for AI document processing (optional)

## Getting Started

### Local Development

1. **Clone the repository**:
```bash
git clone https://github.com/amosproj/amos2025ss02-building-documentation-management-system.git
cd BitAndBeam/backend/src
```

2. **Trust the development HTTPS certificate** (only needed once per machine):
```bash
dotnet dev-certs https --trust
```

3. **Update the connection string** in `appsettings.Development.json` if using a local PostgreSQL instance

4. **Restore dependencies**:
```bash
dotnet restore
```

5. **Run the project**:
```bash
dotnet run
```

6. **Access the API**:
   - Swagger UI: https://localhost:5001/swagger
   - Health check: https://localhost:5001/healthz

### Docker Development

For a complete development environment with PostgreSQL, Tika, and other services:

```bash
cd BitAndBeam
docker compose up
```

The backend will be available at: http://localhost:5001

### Authentication

🔐 **Login**: `POST /auth/login`
```json
{
  "email": "test@example.com",
  "password": "password123"
}
```
Returns a JWT token for authenticated requests.

🔒 **Authenticated Requests**:
Add header: `Authorization: Bearer <your-token>`

🔒 **Logout**: `POST /auth/logout`

---

## API Structure

### Authentication & Security

- **JWT Authentication**: The API uses JSON Web Tokens for authentication
- **Token Validity**: Sessions expire after 1 hour of inactivity
- **Protected Resources**: All endpoints require authentication except:
  - `/auth/login` - For obtaining tokens
  - `/healthz` - For health monitoring
  - `/swagger/*` - API documentation
  
### Multi-Tenancy

The API implements strict multi-tenancy through the Organization model. Users can only access data within their own organization, enforced at both the API and database levels.

### Key Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /buildings` | List buildings for current organization |
| `POST /documents/upload` | Upload new document |
| `GET /documents/{id}` | Get document details |
| `POST /auth/login` | Authenticate user |
| `POST /auth/logout` | End session |
| `GET /healthz` | Service health status |


## Data Models

### Core Entities

#### Organization
The `Organization` entity represents the top-level tenant in our multi-tenant architecture.

**Fields:**
- `OrganizationId` (int, PK) - Primary identifier
- `Name` (string) - Unique organization name
- `Description` (string) - Optional description
- `CreatedAt` (DateTime) - Creation timestamp
- `IsActive` (bool) - Activity status

**Relationships:**
- One-to-many with `User` entities
- One-to-many with `Building` entities

#### Building
Buildings represent physical structures managed by an organization.

**Fields:**
- `BuildingId` (int, PK) - Primary identifier
- `OrganizationId` (int, FK) - Owner organization reference
- `Name` (string) - Building name
- `Address` entities - Structured address information
- Additional building metadata

#### Document
Documents represent files uploaded to the system and their extracted metadata.

**Fields:**
- `DocumentId` (int, PK) - Primary identifier
- `BuildingId` (int, FK, nullable) - Associated building (if any)
- `OrganizationId` (int, FK) - Owner organization reference
- `FileName` (string) - Original file name
- `MimeType` (string) - Document content type
- `FileSize` (long) - Size in bytes
- `StoragePath` (string) - Internal storage location
- `TextContent` (string) - Extracted text content
- `ClassificationType` (enum) - AI-determined document type
- `CreatedAt` (DateTime) - Upload timestamp

### Security Model

Access control is enforced through:
1. JWT-based authentication at the API level
2. Organization-based data filtering in queries
3. Explicit checks in business logic

No user can access data from an organization they don't belong to.

## Development Guide

### Code Style and Formatting

This project uses standard C# and .NET formatting conventions. To ensure your code follows the established patterns:

```bash
# Check formatting compliance before committing
dotnet format --verify-no-changes

# Fix formatting issues automatically
dotnet format
```

### Migrations & Database Updates

When making model changes, create and apply migrations:

```bash
# Create a new migration
dotnet ef migrations add YourMigrationName

# Apply migrations to database
dotnet ef database update
```

### Document Processing Pipeline

The document processing flow works as follows:

1. Document is uploaded through `/documents/upload` endpoint
2. File is saved to document storage
3. Apache Tika extracts text and metadata
4. For scanned documents, OCR is applied automatically
5. AI classification is applied through Ollama integration
6. Document metadata is stored in the database
7. Text content is made available for search indexing

## Additional Notes

- **Ports**: 5000 (HTTP) and 5001 (HTTPS) are used by default
- **HTTPS**: Development HTTPS requires the dev certificate to be trusted
- **Health Checks**: Can be customized in `Program.cs`
- **Line Endings**: Use LF (\n) for Dockerfile and shell scripts
- **Docker**: When using Docker, remember to rebuild images after significant changes
- **Configuration**: See `appsettings.json` for configurable options

## Troubleshooting

- If you encounter HTTPS issues, check firewall settings and certificate trust
- For database connection problems, verify the connection string in appsettings.json
- If document processing fails, check Tika and Ollama service availability

## Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [JWT Authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)


