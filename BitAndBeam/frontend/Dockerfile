# Step 1: Build Angular app
FROM node:20-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install -g @angular/cli && npm install
COPY . .
RUN npm run build --configuration=production

# Step 2: Serve it with NGINX
FROM nginx:alpine

# Clean the default nginx content
RUN rm -rf /usr/share/nginx/html/*

# Copy built Angular app
COPY --from=builder /app/dist/building-ui/browser/ /usr/share/nginx/html/

# Copy nginx configuration (optional if customized)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy env template and rename to env.js temporarily
COPY src/assets/env.template.js /usr/share/nginx/html/assets/env.js

# Replace variables at runtime
CMD ["/bin/sh", "-c", "envsubst < /usr/share/nginx/html/assets/env.js > /usr/share/nginx/html/assets/env.temp.js && mv /usr/share/nginx/html/assets/env.temp.js /usr/share/nginx/html/assets/env.js && nginx -g 'daemon off;'"]

EXPOSE 80

#RUN rm -rf /usr/share/nginx/html/*
