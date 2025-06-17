FROM nginx:alpine

# Install dos2unix
RUN apk add --no-cache dos2unix

# Copy static frontend files
COPY dist/building-ui/browser /usr/share/nginx/html/browser

# Copy entrypoint script
COPY docker-entrypoint.sh /docker-entrypoint.sh

# Normalize line endings and make it executable
RUN dos2unix /docker-entrypoint.sh && chmod +x /docker-entrypoint.sh

# Set entrypoint
ENTRYPOINT ["/docker-entrypoint.sh"]
