FROM python:3.10-slim

# Install Ollama dependencies
RUN apt-get update && apt-get install -y curl gnupg && \
    curl -fsSL https://ollama.com/install.sh | sh && \
    apt-get clean

# Copy app files
WORKDIR /app
COPY app/ .

# Install Python packages
RUN pip install --no-cache-dir -r requirements.txt

# Start Ollama + FastAPI server
CMD ["sh", "-c", "ollama serve & sleep 8 && ollama pull gemma3:1b && uvicorn main:app --host 0.0.0.0 --port 8000"]

