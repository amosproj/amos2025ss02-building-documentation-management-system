from fastapi import FastAPI, Request
from pydantic import BaseModel
import requests

app = FastAPI()

class PromptRequest(BaseModel):
    prompt: str

@app.post("/ask")
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
