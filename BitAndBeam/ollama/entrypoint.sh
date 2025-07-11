#!/bin/sh

# Start Ollama server
ollama serve &

# Wait for it to be ready
sleep 10

# # Remove all existing models
# ollama list --quiet | xargs -r -n 1 ollama rm

# echo "Remaining models:"
# ollama list

# Pull base model
ollama pull gemma3:4b

# Create custom named model (overwrite if exists)
# ollama cp gemma3:4b gemma3-4b:latest

# Create custom model with 16384 context tokens
# ollama create gemma3-4b-16k -f /app/Modelfile

# Keep container alive
wait