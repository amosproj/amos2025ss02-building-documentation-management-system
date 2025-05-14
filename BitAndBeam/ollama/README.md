# Ollama AI Setup

This directory sets up a FastAPI-based microservice inside a Docker container that wraps the Ollama LLM engine.
It provides a clean /ask endpoint to query the gemma3:1b model via HTTP (e.g., from Postman or your backend).

---

## ✅ Features

- Dockerized Ollama LLM runtime with FastAPI wrapper
- Automatically pulls and loads the `gemma3:1b` model
- `/ask` endpoint to send prompts and get responses
- Exposes:
  - Ollama internal API on port `11434`
  - FastAPI external API on port `8000`
- Runs with no external APIs or API keys

---

## 🚀 How to Run

To build and run this service using Docker,  
please follow the instructions in the Docker setup guide:

🔗 [Ollama Docker Setup Guide](./README_DOCKER.md)

---

## 📁 Project Structure

```
ollama/
├── app/
│   ├── main.py              # FastAPI app with /ask endpoint
│   └── requirements.txt     # Python dependencies
├── Dockerfile               # Builds Ollama + FastAPI combo
├── README.md
├── README_DOCKER.md
```

---