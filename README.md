<div align="center">
  <img src="Deliverables/sprint-01/team-logo.png" alt="Bit&Beam Logo" width="250">

# Bit&Beam

### Intelligent Document Management System for Building Data

  <p align="center">
    A modern solution for organizing, analyzing, and retrieving building-related documents with AI-powered features
  </p>
</div>

![Start web service](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/docker-ci.yml/badge.svg?branch=main&nocache=1)
![Frontend Linting](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/frontend-lint.yml/badge.svg?nocache=1)
![Backend Linting](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/backend-lint.yml/badge.svg?nocache=1)
![OpenAPI Client Generation](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/openapi-client.yml/badge.svg?nocache=1)

## Quick Links

- **Production Web Service**: [amos.b-iq.net](http://amos.b-iq.net/)


## Project Mission

We aim to build a secure, multi-tenant backend that stores uploaded permits, certificates, and maintenance reports; applies AI-driven OCR and metadata extraction to classify and validate each document; and provides a web UI where users can query and filter their building records using plain language. By implementing and demonstrating a full workflow — from document upload to automatic processing and natural-language search on sample data — we will create the technical foundation for a production-ready system that significantly reduces the time and effort required to manage building documents.

## Overview

Bit&Beam is an intelligent document management system designed specifically for building-related data. The system streamlines document workflows by providing automated classification, metadata extraction, and smart search capabilities, making it easier for construction professionals and building administrators to manage critical documentation.

## Key Features

- **Document Management**
  - Secure storage and organization of building-related documents
  - Version control and document history tracking
  - Multi-format support (PDF, Office documents, images)

- **Multi-Tenant Architecture**
  - Organization-based data isolation
  - Role-based access control
  - Secure authentication with JWT

- **AI-Powered Processing**
  - Automated document classification using LLMs
  - OCR for scanned documents with multi-language support (English, German, French)
  - Metadata extraction from document content
  
- **Search & Analytics**
  - Natural language querying
  - Advanced filtering and sorting options
  - Document relationships and building-specific views

- **User Interface**
  - Modern, responsive Angular frontend
  - Drag-and-drop document upload
  - Interactive document validation and classification interface

## System Architecture

### Tech Stack

- **Frontend:** 
  - Angular 19 (TypeScript)
  - Modern UI with responsive design
  - OpenAPI client generation for type-safe API integration

- **Backend:** 
  - C# 8 with ASP.NET Core
  - RESTful API with Swagger documentation
  - JWT authentication and authorization
  
- **Database:** 
  - PostgreSQL 17 for structured data storage
  - Entity Framework Core for ORM
  - Multi-tenant data model

- **AI & Document Processing:**
  - Apache Tika + Tesseract OCR for document extraction
  - Ollama for LLM-based classification and analysis
  - Custom document processing pipeline

- **DevOps:**
  - Docker containerization for all services
  - CI/CD via GitHub Actions
  - Automated testing and linting

## Project Structure

```
/BitAndBeam
│
├── backend/                # ASP.NET Core API (C#)
│   ├── src/                # Main backend source code
│   │   ├── Controllers/    # API endpoints
│   │   ├── Models/         # Domain models
│   │   ├── Services/       # Business logic 
│   │   ├── Migrations/     # Database migrations
│   │   ├── HealthChecks/   # Service health monitoring
│   │   └── Program.cs      # Application entry point
│   ├── README.md           # Backend documentation
│   └── Dockerfile          # Backend container definition
│
├── frontend/               # Angular app (TypeScript)
│   ├── src/                # Frontend source code
│   │   ├── app/            # Angular components
│   │   ├── assets/         # Static assets
│   │   └── api/            # Generated API client
│   ├── README.md           # Frontend documentation
│   └── Dockerfile          # Frontend container definition
│
├── openapi-client/         # OpenAPI client generation
│   └── Dockerfile          # Client generator container
│
├── tika/                   # Apache Tika OCR integration
│   ├── tika-config.xml     # OCR configuration
│   ├── README.md           # Tika documentation
│   └── Dockerfile          # Tika container with Tesseract OCR
│
├── ollama/                 # Ollama AI integration
│   ├── Modelfile           # LLM model definition
│   ├── README.md           # Ollama documentation
│   └── Dockerfile          # Ollama container with LLM models
│
├── database/               # Database definitions
│   └── schema.sql          # Initial database schema
│
├── docker-compose.yml             # Development orchestration
├── docker-compose-prod.yml        # Production orchestration
├── docker-compose-prod-ollama.yml # Production with local Ollama
└── docker-compose-prod-ollama-gpu.yml # Production with GPU Ollama
```

## Getting Started

### Prerequisites

- [Docker](https://www.docker.com/products/docker-desktop/) and Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for backend development)
- [Node.js](https://nodejs.org/) v18.19.1+ and [Angular CLI](https://angular.io/cli) (for frontend development)

### Development Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/amosproj/amos2025ss02-building-documentation-management-system.git
   cd amos2025ss02-building-documentation-management-system/BitAndBeam
   ```

2. Start all services in development mode:
   ```bash
   docker compose up
   ```

3. Access the services:
   - Frontend: http://localhost:8080
   - Backend API: http://localhost:5001
   - Swagger API docs: http://localhost:5001/swagger
   - Backend health check: http://localhost:5001/healthz

### Component-Specific Development

#### Backend Development

```bash
cd BitAndBeam/backend/src
dotnet restore
dotnet run
```

#### Frontend Development

```bash
cd BitAndBeam/frontend
npm install
ng serve
```

### Production Deployment

1. Configure deployment environment:
   - Setup GitHub Secrets: PROJECT_SERVER_IP, SSH_USER, SSH_PRIVATE_KEY

2. Deploy using docker compose:
   ```bash
   cd BitAndBeam
   docker compose -f docker-compose-prod.yml up -d
   ```

3. For GPU-accelerated Ollama:
   ```bash
   docker compose -f docker-compose-prod-ollama-gpu.yml up -d
   ```

4. Access production services:
   - Web UI: http://amos.b-iq.net
   - Backend API: http://amos.b-iq.net:5000
   - Swagger API docs: http://amos.b-iq.net:5000/swagger
   
## Monitoring & Operations

### Health Checks

- **Backend API Health**: http://amos.b-iq.net:5000/healthz
- **Ollama LLM Status**: [amos-gpu.b-iq.net:11434/api/tags](http://amos-gpu.b-iq.net:11434/api/tags)
- **Tika OCR Service**: http://localhost:9998/version (in development)

### CI/CD Status

- [Docker CI Workflow](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/docker-ci.yml)
- [Frontend Linting](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/frontend-lint.yml)
- [Backend Linting](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/backend-lint.yml)
- [OpenAPI Client Generation](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/openapi-client.yml)

### Logs & Debugging

```bash
# View all container logs
docker compose -f docker-compose-prod.yml logs

# View specific service logs
docker compose -f docker-compose-prod.yml logs backend
docker compose -f docker-compose-prod.yml logs frontend

# Follow logs in real-time
docker compose -f docker-compose-prod.yml logs -f backend
```

## System Features

### Document Processing Pipeline

Bit&Beam implements a sophisticated document processing pipeline:

1. **Document Upload**: Multi-format upload through the Angular frontend
2. **Initial Extraction**: Apache Tika extracts text and metadata
3. **OCR Processing**: Documents with insufficient text are processed using Tesseract OCR
4. **Classification**: LLM-based document type classification
5. **Metadata Extraction**: Structured information extraction from documents
6. **Storage**: Documents and metadata stored in PostgreSQL and document storage
7. **Indexing**: Content indexed for natural language search capabilities

### Multi-Tenant Security Model

The system is designed with strict multi-tenancy in mind:

- Organizations provide the top-level isolation boundary
- Users belong to a single organization and can only access their organization's data
- Buildings are associated with organizations, creating a logical data hierarchy
- JWT-based authentication with role-based permissions

### OCR Capabilities

The OCR pipeline uses Apache Tika with Tesseract and supports:

- Multiple languages (English, German, French)
- Automatic language detection
- Performance optimizations for large documents
- Fallback strategies for different document types

## Documentation

Detailed documentation for each component is available in the respective README files:

- [Backend Documentation](BitAndBeam/backend/README.md)
- [Frontend Documentation](BitAndBeam/frontend/README.md)
- [Tika OCR Documentation](BitAndBeam/tika/README.md)
- [Ollama AI Documentation](BitAndBeam/ollama/README.md)

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch
3. Make your changes following the [Coding Guidelines](Coding_Guidelines.md)
4. Run linting and tests
5. Submit a pull request

---

## License

MIT License
