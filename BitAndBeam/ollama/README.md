<div align="center">
  <h1>Ollama AI Integration</h1>
  <p>Large Language Model Engine for BitAndBeam Document Processing</p>
</div>

![Ollama](https://img.shields.io/badge/Ollama-Latest-purple.svg)
![Model](https://img.shields.io/badge/Model-gemma3:1b-orange.svg)
![Docker](https://img.shields.io/badge/Docker-Enabled-2496ED.svg)

## Overview

The Ollama AI component provides a containerized Large Language Model (LLM) service that powers BitAndBeam's document classification, information extraction, and search capabilities. This module sets up a **Dockerized Ollama LLM engine** with a REST API that can be used directly by the backend application.

## Key Features

- **Pure Dockerized Solution**: Self-contained Ollama runtime with no additional layers
- **Pre-configured Model**: Automatically pulls and loads the `gemma3:1b` model on startup
- **REST API Integration**: Exposes Ollama's native API on port `11434`
- **Serverless Architecture**: No API keys or authentication required for local deployment
- **Customizable**: Easily change models or parameters through configuration

## Installation & Usage

### Running with Docker Compose

The recommended way to run the Ollama service is with Docker Compose as part of the BitAndBeam stack:

```bash
# Start Ollama with the rest of the stack
docker compose up -d

# Or start just the Ollama service
docker compose up -d ollama
```

### Running Standalone

You can also build and run the service independently:

```bash
# Build the custom image
docker build -t bitandbeam-ollama ./ollama

# Run the container
docker run -d --name ollama -p 11434:11434 bitandbeam-ollama
```

### GPU Acceleration

For GPU acceleration (recommended for production):

```bash
# With Docker Compose
docker compose -f docker-compose-prod.yml up -d ollama

# Or standalone with GPU
docker run -d --name ollama --gpus all -p 11434:11434 bitandbeam-ollama
```

## Project Structure

```
ollama/
├── Dockerfile               # Builds Ollama container and pulls default model
├── entrypoint.sh            # Pulls model on startup and configures runtime
├── README.md                # This documentation
```

## API Integration

### Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/chat` | POST | Chat completions API for conversational interactions |
| `/api/generate` | POST | Text generation for prompts |
| `/api/embeddings` | POST | Generate vector embeddings from text |
| `/api/tags` | GET | List available models and status |
| `/api/pull` | POST | Pull a model from the Ollama library |

### Health Check

The service health can be checked using:

```bash
curl http://localhost:11434/api/tags
```

This endpoint returns information about available models and serves as a health check.

## Document Processing Pipeline

The Ollama service is used within BitAndBeam for several AI-powered functions:

1. **Document Classification**: Automatically categorize uploaded documents by type and content
2. **Information Extraction**: Identify key information like dates, project numbers, and contacts
3. **Natural Language Search**: Power semantic search capabilities beyond simple keyword matching
4. **Summary Generation**: Create concise summaries of long documents

## Configuration

### Changing the Default Model

To use a different LLM model:

1. Edit the `Dockerfile` or `entrypoint.sh` to change the model name:
   ```bash
   # Original
   ollama pull gemma3:1b
   
   # Modified for a different model
   ollama pull llama3:8b
   ```

2. Rebuild the container:
   ```bash
   docker compose build ollama
   docker compose up -d ollama
   ```

### Memory and Performance Settings

Ollama resource allocation can be adjusted in the Docker Compose file:

```yaml
services:
  ollama:
    deploy:
      resources:
        limits:
          memory: 4G  # Adjust based on model size
```

## Troubleshooting

### Common Issues

| Problem | Possible Solutions |
|---------|--------------------|
| Slow responses | Enable GPU support or increase CPU/memory allocation |
| Container crashes | Check memory limits, may need more for larger models |
| Network errors | Verify port 11434 is properly exposed and not blocked |
| Model loading fails | Check available disk space for model storage |

### Container Logs

To view the Ollama container logs:

```bash
docker compose logs -f ollama
```

## Resources

- [Ollama Documentation](https://ollama.ai/docs)
- [Ollama API Reference](https://ollama.ai/docs/api)
- [Ollama Model Library](https://ollama.ai/library)
- [Gemma LLM Documentation](https://ai.google.dev/gemma)
