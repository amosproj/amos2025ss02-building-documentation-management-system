FROM ollama/ollama:latest

# Install dos2unix
RUN apt-get update && apt-get install -y dos2unix && apt-get clean

# Set working directory
WORKDIR /app

# Copy files
# COPY Modelfile /app/Modelfile
COPY entrypoint.sh /entrypoint.sh

# Convert entrypoint script to LF and make executable
RUN dos2unix /entrypoint.sh && chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]