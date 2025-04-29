# Backend (ASP.NET Core)

This directory contains the backend API for the Intelligent Document Management System.

## Responsibilities
- Exposes RESTful endpoints for document management, user authentication, and metadata extraction
- Integrates with PostgreSQL for data storage and multi-tenancy
- Connects to Opensearch for intelligent search
- Uses Apache Tika and Ollama for document text extraction and AI-powered classification

## Stack
- C#
- ASP.NET Core Web API
- PostgreSQL
- Opensearch
- Apache Tika
- Ollama

## Getting Started
- The backend will be containerized using Docker
- See the main project README for orchestration details

---
Add controllers, models, and services in the `src/` directory. Add tests in the `tests/` directory.
