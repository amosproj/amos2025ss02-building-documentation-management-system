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

-> 🔐 Login endpoint: POST https://localhost:5001/auth/login  
   - Body: `{ "email": "test@example.com", "password": "password123" }`
   - Returns a JWT token used for authenticated requests

-> 🔒 Logout endpoint: POST https://localhost:5001/auth/logout  
   - Requires Authorization header: `Bearer <token>`

---

## 🔐 Authentication Notes

- The API uses **JWT-based authentication**.
- To access protected endpoints, you must:
  - First log in via `/auth/login`
  - Then include the token in the `Authorization` header of all subsequent requests:  
    `Authorization: Bearer <your-token>`
- Sessions expire after 1 hour of inactivity.
- All endpoints (except `/auth/login` and `/healthz`) require authentication.


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

-> Set LF end-of-line sequence for Dockerfile and .sh files, especially in backend directory

# BitandBeam: Secure Deployment with HTTPS

This describes how to configure, deploy, and test HTTPS support for the BitandBeam system using Traefik, Let's Encrypt, Docker, and DuckDNS.

---

## 🔐 HTTPS Deployment Overview

- ✅ Reverse proxy: **Traefik v2**
- ✅ Free SSL/TLS: **Let's Encrypt (ACME)**
- ✅ Domain: **amos.b-iq.net**
- ✅ Automatic HTTP → HTTPS redirection
- ✅ Services routed via path-based routing (e.g. /, /api, /ollama)
- ✅ Port 80/443 exposed via Docker & router

---

## ⚙️Setup Overview
- Docker Compose used to define services

- Traefik handles HTTPS termination and routing based on path

- Let's Encrypt provides valid, auto-renewed certificates

- DNS A-record for amos.b-iq.net points to the public server IP

---
✅ Global Functionality Testing

🔹 Test 1: HTTPS and Certificate Validity

curl -v https://amos.b-iq.net

✅ Expect:

SSL certificate verify ok.

Response from your frontend or landing page

Or simply open https://amos.b-iq.net in a browser

You should see a 🔒 padlock icon

Certificate should be issued by Let's Encrypt

🔹 Test 2: HTTP Redirects to HTTPS

curl -I http://amos.b-iq.net

✅ Expect:

HTTP/1.1 308 Permanent Redirect
Location: https://amos.b-iq.net

🔹 Test 3: Path-Based Routing for Services

Functionality                 Path                    How to Test

Frontend UI                     /                     Open in browser
 
Backend API                   /api/...                Use curl or your app

Ollama Service                /ollama/...             Use HTTP client or test endpoint
 
Example:

curl -X GET https://amos.b-iq.net/api/health
curl -X GET https://amos.b-iq.net/ollama/health

✅ Expect 200 OK responses

🔹 Test 4: Automatic Certificate Storage

docker exec traefik cat /letsencrypt/acme.json

✅ You should see JSON entries showing that Let's Encrypt certs have been saved.