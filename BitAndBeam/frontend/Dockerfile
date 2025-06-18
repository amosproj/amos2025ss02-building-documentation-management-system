# Step 1: Build Angular app
FROM node:20-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install -g @angular/cli && npm install
COPY . .
RUN npm run build --configuration=production

# Step 2: Serve it with NGINX
FROM nginx:alpine

# Install required tools
RUN apk add --no-cache gettext

# Create necessary directories and set permissions
RUN mkdir -p /usr/share/nginx/html && \
    mkdir -p /tmp/client_temp && \
    mkdir -p /tmp/proxy_temp && \
    mkdir -p /tmp/fastcgi_temp && \
    mkdir -p /tmp/uwsgi_temp && \
    mkdir -p /tmp/scgi_temp && \
    chown -R nginx:nginx /usr/share/nginx && \
    chown -R nginx:nginx /tmp && \
    chmod -R 755 /tmp

# Copy built Angular app
COPY --from=builder /app/dist/building-ui/browser /usr/share/nginx/html/
RUN chown -R nginx:nginx /usr/share/nginx/html

# Copy nginx configurations
COPY nginx.main.conf /etc/nginx/nginx.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy entrypoint script and ensure it has the correct permissions
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh && \
    sed -i 's/\r$//' /docker-entrypoint.sh

# Switch to non-root user
USER nginx

EXPOSE 80

ENTRYPOINT ["/docker-entrypoint.sh"]
