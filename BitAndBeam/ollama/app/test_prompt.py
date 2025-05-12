import requests
import time

# Retry in case Ollama API is not ready instantly
for i in range(5):
    try:
        response = requests.post(
            "http://localhost:11434/api/generate",
            json={
                "model": "gemma3:1b",
                # "model": "llama3",
                "prompt": "What is the capital of Germany?",
                "stream": False
            }
        )
        print("Prompt: What is the capital of Germany?")
        print("Response:", response.json()["response"])
        break
    except Exception as e:
        print("Retrying... (Ollama not ready yet)", str(e))
        time.sleep(3)
