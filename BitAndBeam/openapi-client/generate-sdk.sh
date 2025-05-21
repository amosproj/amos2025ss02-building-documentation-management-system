#!/bin/sh

echo "Waiting for backend Swagger spec..."
until wget -qO /tmp/swagger.json http://backend:5000/swagger/v1/swagger.json; do
  echo "Waiting..."
  sleep 3
done

echo "Swagger spec available. Generating SDK..."
openapi-generator-cli generate \
  -i /tmp/swagger.json \
  -g typescript-axios \
  -o /local/generated-sdk
