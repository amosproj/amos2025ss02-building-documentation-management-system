FROM python:3.10-slim

# Install Ollama dependencies
RUN apt-get update && apt-get install -y curl gnupg && \
    curl -fsSL https://ollama.com/install.sh | sh && \
    apt-get clean

# Copy app files
WORKDIR /app
COPY app/ .

# Install Python requirements
RUN pip install --no-cache-dir -r requirements.txt

# Pull the model (gemma3:1b) and run test script
CMD ollama serve & \
    sleep 8 && \
    ollama pull gemma3:1b && \
    python test_prompt.py && \
    tail -f /dev/null

    
# Pull the model (llama3) and run test script
# CMD ollama serve & \
# sleep 8 && \
# ollama pull llama3 && \
# python test_prompt.py && \
# tail -f /dev/null
