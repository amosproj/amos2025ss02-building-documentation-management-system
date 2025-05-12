# Ollama AI Setup

This directory sets up a fully Dockerized [Ollama](https://ollama.com) application with the `gemma3:1b` model.  
It includes a test script to send a prompt and return a response from the model using the local HTTP API.

---

## ✅ Features

- Installs and runs **Ollama** inside a Docker container
- Automatically pulls the `gemma3:1b` model
- Sends a test prompt to the Ollama HTTP API from inside the container
- Exposes API on port `11434` for external access
- No external APIs required

---

## 🚀 How to Run

### 1. Build and Start Container

```bash
docker-compose up --build
````

> This will:
>
> * Build the container
> * Start Ollama
> * Pull the `gemma3:1b` model
> * Send a test prompt: `"What is the capital of Germany?"`

---

### 2. Expected Output

```bash
Prompt: What is the capital of Germany?
Response: Berlin
```

---

## ⚙️ API Details

* **Base URL**: `http://localhost:11434`
* **Endpoint**: `/api/generate`
* **Method**: `POST`

### Example Payload:

```json
{
  "model": "gemma3:1b",
  "prompt": "What is the capital of Germany?",
  "stream": false
}
```

---

## 📂 Folder Structure

```
ollama/
├── app/
│   ├── test_prompt.py
│   └── requirements.txt
├── Dockerfile
├── docker-compose.yml
├── README.md
```

---

## 🧪 Notes

* You can change the model in `test_prompt.py` and Dockerfile (e.g., to `llama3`, `mistral`, etc.)
* To stop the container:

```bash
docker stop ollama
docker rm ollama
```