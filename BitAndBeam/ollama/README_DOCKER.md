
# 🐳 Docker Guide for Ollama AI Microservice

This guide helps you run the **Ollama AI microservice** using Docker.  
It includes both development and production setups, which are managed from the root `BitAndBeam/` directory.

---

## 🧰 Prerequisites

Make sure Docker is installed and running on your system.

### 🔗 Install Docker

- **Windows/macOS**:  
  https://www.docker.com/products/docker-desktop

- **Linux (Ubuntu/Debian)**:
```bash
sudo apt update
sudo apt install docker.io
sudo systemctl start docker
sudo systemctl enable docker
````

> 🔁 You may need to restart your system or log out/in after installing Docker

---

## ▶️ Run in Development Mode

> Make sure you're in the **BitAndBeam/** root directory (not inside `ollama/`)

```bash
docker compose up --build
```

This will:

* Build the Ollama container from `./ollama/Dockerfile`
* Install Python, FastAPI, and Ollama runtime
* Pull the `gemma3:1b` model
* Expose two ports:

  * `11434` – Ollama's native API
  * `8000` – FastAPI wrapper (`/ask` endpoint)

---

## 🚀 Run in Production Mode

> Still from the **BitAndBeam/** root directory:

```bash
docker compose -f docker-compose-prod.yml up --pull always
```

This will:

* Pull the production-ready Ollama image from GHCR
* Start the FastAPI + Ollama service as a container
* Make it available at:

```
http://localhost:8000/ask       ← POST prompt API
http://localhost:8000/docs      ← FastAPI Swagger UI (interactive)
```

---

## 🌐 API Testing

After starting the container, open:

* 🔎 **FastAPI Swagger UI**:
  `http://localhost:8000/docs`

From here you can interactively test the `/ask` endpoint.

---

## 🔁 Example curl Request

```bash
curl -X POST http://localhost:8000/ask \
  -H "Content-Type: application/json" \
  -d '{"prompt": "What is the capital of Germany?"}'
```

Expected response:

```json
{
  "response": "Berlin"
}
```

---

## 🧼 Stop and Remove Ollama Container

```bash
docker stop ollama
docker rm ollama
```

---

## 📝 Notes

* You can change the default model in `app/main.py`
  Just replace `"gemma3:1b"` with another model like `"llama3"`, `"mistral"`, etc.
* If you update the model, don’t forget to rebuild:

```bash
docker compose build ollama
```

---
