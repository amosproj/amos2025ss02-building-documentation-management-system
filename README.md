<div align="center">
  <img src="team-logo.png" alt="Bit&Beam Logo" width="250">

# Bit&Beam

### Intelligent Document Management System for Building Data

  <p align="center">
    A modern solution for organizing, analyzing, and retrieving building-related documents with AI-powered features
  </p>
</div>

![Start web service](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/docker-ci.yml/badge.svg?branch=main&nocache=1)
![Frontend Linting](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/frontend-lint.yml/badge.svg?nocache=1)
![Backend Linting](https://github.com/amosproj/amos2025ss02-building-documentation-management-system/actions/workflows/backend-lint.yml/badge.svg?nocache=1)

Web service available at: [amos.b-iq.net](http://amos.b-iq.net/)

## Project Mission

We aim to build a secure, multi-tenant backend that stores uploaded permits, certificates, and maintenance reports; applies AI-driven OCR and metadata extraction to classify and validate each document; and provides a web UI where users can query and filter their building records using plain language. By implementing and demonstrating a full workflow — from document upload to automatic processing and natural-language search on sample data — we will create the technical foundation for a production-ready system that significantly reduces the time and effort required to manage building documents.

## Overview

Bit&Beam is an intelligent document management system designed specifically for building-related data. The system streamlines document workflows by providing automated classification, metadata extraction, and smart search capabilities, making it easier for construction professionals and building administrators to manage critical documentation.

## Features

-   Store, organize, and manage building-related documents
-   Maintain a structured database of various document types (permits, certificates, reports)
-   Multi-tenancy: Groups only access their assigned data
-   Automated document data extraction (metadata and text fields)
-   Automated document categorization
-   UI for classification & validation
-   Natural language querying and intelligent search

## Tech Stack

-   **Frontend:** Angular (TypeScript)
-   **Backend:** C# + ASP.NET Core
-   **Database:** PostgreSQL (with pgai)
-   **Search:** Opensearch
-   **AI/Extraction:** Ollama, Apache Tika
-   **Containerization:** Docker

## Project Structure

```
/BitAndBeam
│
├── backend/                # ASP.NET Core API (C#)
│   ├── BUILD.ING/          # Main backend source code and migrations
│   └── Dockerfile
│
├── frontend/               # Angular app (TypeScript)
│   ├── src/
│   ├── public/
│   └── Dockerfile
│
├── opensearch/             # Opensearch config/scripts
│   └── README.md
│
├── postgres/               # PostgreSQL init scripts, pgai setup
│   └── init.sql
│
├── tika/                   # Apache Tika integration/config
│   └── README.md
│
├── ollama/                 # Ollama AI integration/config
│   └── README.md
│
├── web/                    # Static web content
│   ├── Dockerfile
│   └── index.html
│
├── Instructions/           # Project instructions and sprint docs
│
├── database/               # Database schema and diagrams
│   ├── database_diagram.dbml
│   └── schema.sql
│
├── docker-compose.yml      # Orchestration for all services
├── docker-compose-prod.yml # Production orchestration
├── README.md               # Project root readme
```

## Getting Started

1. Clone the repository
2. Follow setup instructions in each service directory
3. Use `docker compose up` inside BitAndBeam, to start all services in development mode
4. Use `docker compose -f docker-compose-prod.yml up` inside BitAndBeam, to start all services in production mode
5. Setup GitHub Secrets for PROJECT_SERVER_IP, SSH_USER, SSH_PRIVATE_KEY to trigger web service start by GitHub Actions on push to main
6. Access web service at [amos.b-iq.net](http://amos.b-iq.net/) after successful Github Actions workflow. Use port 5000 for backend, 8080 for frontend, 8000/docs for ollama. Postgres is not a web service, hence not accessible.

## Contributing

-   Please see `CONTRIBUTING.md` (to be added)

---

## License

MIT License
