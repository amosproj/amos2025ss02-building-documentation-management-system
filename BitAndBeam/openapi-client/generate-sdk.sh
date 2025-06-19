#!/bin/sh

MAX_RETRIES=10
RETRY_COUNT=0

echo "Waiting for backend Swagger spec at ${SWAGGER_URL}/swagger/v1/swagger.json..."

until wget -qO /tmp/swagger.json "${SWAGGER_URL}/swagger/v1/swagger.json"; do
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
        echo "Error: Failed to fetch Swagger spec after $MAX_RETRIES attempts"
        exit 1
    fi
    echo "Waiting... (Attempt $RETRY_COUNT of $MAX_RETRIES)"
    sleep 3
done

echo "Swagger spec available. Generating SDK..."
openapi-generator-cli generate \
  -i /tmp/swagger.json \
  -g typescript-axios \
  -o /local/generated-sdk \
  --additional-properties=baseUrl=${BACKEND_URL}

# Ensure the generated configuration uses the runtime environment URL
echo "Updating configuration to use runtime environment URL..."
sed -i 's|baseURL: ".*"|baseURL: window.__env.API_URL|g' /local/generated-sdk/configuration.ts
