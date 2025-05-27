from fastapi import FastAPI
from pydantic import BaseModel
from fastapi.middleware.cors import CORSMiddleware
import requests

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=["http://localhost:8080"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class PromptRequest(BaseModel):
    prompt: str

@app.post("/api/Ollama/ask")  # ✅ This is the key fix!
def ask_llm(req: PromptRequest):
    response = requests.post(
        "http://localhost:11434/api/generate",
        json={
            "model": "gemma3:1b",
            "prompt": req.prompt,
            "stream": False
        }
    )
    return {"response": response.json()["response"]}

@app.get("/health")
async def health():
    return {"status": "ok"}
