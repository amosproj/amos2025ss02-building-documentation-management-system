# Bit&Beam: Intelligent Document Management System

This project is an intelligent document management system for building-related data. It enables automated document classification, metadata extraction, and user access control to streamline document management in building administration.

## Features
- Store, organize, and manage building-related documents
- Maintain a structured database of various document types (permits, certificates, reports)
- Multi-tenancy: Groups only access their assigned data
- Automated document data extraction (metadata and text fields)
- Automated document categorization
- UI for classification & validation (Google Notebook LM style)
- Natural language querying and intelligent search

## Tech Stack
- **Frontend:** Angular (TypeScript)
- **Backend:** C# + ASP.NET Core
- **Database:** PostgreSQL (with pgai)
- **Search:** Opensearch
- **AI/Extraction:** Ollama, Apache Tika
- **Containerization:** Docker

## Project Structure
```
/BitAndBeam
│
├── backend/                # ASP.NET Core API (C#)
│   ├── src/
│   ├── tests/
│   └── Dockerfile
│
├── frontend/               # Angular app (TypeScript)
│   ├── src/
│   ├── e2e/
│   └── Dockerfile
│
├── opensearch/             # Opensearch config/scripts
│
├── postgres/               # PostgreSQL init scripts, pgai setup
│
├── tika/                   # Apache Tika integration/config
│
├── ollama/                 # Ollama AI integration/config
│
├── docker-compose.yml      # Orchestration for all services
/README.md
/.github/                # CI/CD workflows (GitHub Actions)
```

## Getting Started
1. Clone the repository
2. Follow setup instructions in each service directory
3. Use `docker-compose up` to start all services

## Contributing
- Please see `CONTRIBUTING.md` (to be added)

---

## License
MIT License