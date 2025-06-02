# Apache Tika Server

This directory contains the configuration for Apache Tika, a powerful document parsing and text extraction tool used in the Bit&Beam document management system.

## Overview

Apache Tika is implemented as a REST API server running in a Docker container. It provides the following capabilities:

- Document text extraction from various file formats (PDF, DOC, DOCX, PPT, PPTX, XLS, XLSX, etc.)
- Metadata extraction (author, creation date, modified date, etc.)
- MIME type detection
- Language detection

## Configuration

- **Port**: The service runs on port 9998 (configured in docker-compose.yml)
- **Heap Size**: Configured with 2GB maximum heap size to handle large documents
- **CORS**: Enabled for all origins to allow cross-domain requests
- **Health Check**: Configured to verify server availability

## Integration with Backend

The Tika service is integrated with the C# backend through the `TikaService` and `TikaController` classes, which provide:

- REST API for document text extraction
- Proper error handling for various failure scenarios
- Consistent error response format

## API Endpoints

The Tika server exposes several endpoints:

- `/tika`: General text extraction endpoint
- `/tika/text`: Extracts plain text content
- `/tika/metadata`: Extracts only metadata from documents
- `/tika/rmeta`: Returns JSON metadata
- `/version`: Returns the Tika server version

## Usage Examples

### Text Extraction

```bash
curl -T document.pdf http://localhost:9998/tika
```

### Metadata Extraction

```bash
curl -T document.pdf http://localhost:9998/meta
```

## Custom Configuration

For custom Tika configurations or processing pipelines, add your configuration files to this directory and update the Dockerfile accordingly.
