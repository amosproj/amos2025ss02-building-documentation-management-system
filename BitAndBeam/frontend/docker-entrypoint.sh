#!/bin/sh

set -e

echo "Generating env.js..."

cat <<EOF > /usr/share/nginx/html/browser/assets/env.js
window.__env = {
  API_URL: "${API_URL}"
};
EOF

echo "env.js content:"
cat /usr/share/nginx/html/browser/assets/env.js

# Start nginx in foreground
exec nginx -g 'daemon off;'
