#!/bin/sh

# Exit on any error
set -e

echo "Starting entrypoint script..."

# Create assets directory in the browser directory if it doesn't exist
mkdir -p /usr/share/nginx/html/browser/assets

# Create or update env.js in the browser/assets directory
echo "Updating environment variables in env.js..."
echo "window.__env = { API_URL: \"${API_URL}\" };" > /usr/share/nginx/html/browser/assets/env.js

echo "env.js content:"
cat /usr/share/nginx/html/browser/assets/env.js

# Start nginx
echo "Starting nginx..."
exec nginx -g 'daemon off;'
