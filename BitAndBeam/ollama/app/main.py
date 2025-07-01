from fastapi import FastAPI, HTTPException, Response
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import httpx
import asyncio

# ──────────────────────────────────────────────────────────────────────────
#  Settings
# ──────────────────────────────────────────────────────────────────────────
OLLAMA_URL   = "http://localhost:11434/api/generate"
# OLLAMA_MODEL = "gemma3:latest"          # change once, use everywhere
OLLAMA_MODEL = "gemma3:1b"
UPSTREAM_TIMEOUT = 300                  # seconds

# ──────────────────────────────────────────────────────────────────────────
#  FastAPI app
# ──────────────────────────────────────────────────────────────────────────
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
    stream: bool | None = False         # let caller opt-in to server-sent tokens


# ──────────────────────────────────────────────────────────────────────────
#  /api/Ollama/ask
# ──────────────────────────────────────────────────────────────────────────
@app.post("/api/Ollama/ask")
async def ask_llm(req: PromptRequest):
    payload = {
        "model":  OLLAMA_MODEL,
        "prompt": req.prompt,
        "stream": bool(req.stream),
    }

    async with httpx.AsyncClient(timeout=UPSTREAM_TIMEOUT) as client:
        try:
            if req.stream:
                # ───────────── stream tokens back to caller ──────────────
                async with client.stream("POST", OLLAMA_URL, json=payload) as r:
                    r.raise_for_status()

                    async def token_generator():
                        async for chunk in r.aiter_bytes():
                            yield chunk
                    return Response(token_generator(), media_type="application/octet-stream")
            else:
                # ───────────── one-shot request / response ───────────────
                r = await client.post(OLLAMA_URL, json=payload)
                r.raise_for_status()

                data = r.json()
                if "response" in data:                   # ✅ success path
                    return {"response": data["response"]}
                if "error" in data:                      # ❌ model error
                    raise HTTPException(
                        status_code=502,
                        detail=f"Ollama error: {data['error']}"
                    )
                raise HTTPException(502, "Unexpected response from Ollama")

        except httpx.TimeoutException:
            raise HTTPException(504, "Ollama request timed out")
        except httpx.HTTPStatusError as e:
            raise HTTPException(502, f"Upstream HTTP {e.response.status_code}")


# ──────────────────────────────────────────────────────────────────────────
#  Health check
# ──────────────────────────────────────────────────────────────────────────
@app.get("/health")
async def health():
    return {"status": "ok"}
