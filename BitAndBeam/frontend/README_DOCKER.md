# Angular Frontend Dockerization Guide

## 📊 Purpose

This guide documents how to build, run, and configure an Angular frontend as a production-grade Docker image. It is intended for team members who are new to Angular, Docker, or both.

---

## ✅ Criterion 1: Multi-Stage Dockerfile

### 📉 Goal

Use a multi-stage Dockerfile to:

- Build the Angular app using Node.js

- Serve static files with NGINX

- Minimize image size by copying only required files

### 📈 Implementation
**`frontend/Dockerfile`**:
```dockerfile
# Stage 1: Build Angular app
FROM node:20-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install -g @angular/cli && npm install
COPY . .
RUN npm run build --configuration=production

# Stage 2: Serve using NGINX
FROM nginx:alpine
RUN rm -rf /usr/share/nginx/html/*
COPY --from=builder /app/dist/building-ui/browser/ /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### 🔄 Context Check
Ensure `angular.json` has `outputPath` as `dist/building-ui/browser`

---

## ✅ Criterion 2: Serve Application on Port 80

### 🔧 How It Works
- NGINX serves static Angular files on port 80.

- Dockerfile exposes port 80.

- Locally mapped to port 8080 for testing.

### ▶️ Run It
#### Start Docker Desktop (for local setup on Windows)

- Open Start Menu → Search for Docker Desktop → Click to launch it.

- Wait for it to say "Docker is running" (check the tray icon).

- Verify It’s Running

```bash
docker info
```

#### Build and Run

```bash
docker build -t my-angular-app .
docker run --rm -p 8080:80 my-angular-app
```
Then open `http://localhost:8080` in the browser.

---

## ✅ Criterion 3: Runtime Environment Variables

### 🌐 Goal

Allow passing values like `API_URL` **without rebuilding** the Docker image.

### 📁 File: `env.template.js`
```js
window.__env = {
  API_URL: "${API_URL}"
};
```
Put this inside `frontend/assets/env.template.js`

### 📊 Dockerfile Updates
```dockerfile
COPY ./assets/env.template.js /usr/share/nginx/html/assets/env.js
RUN apk add --no-cache gettext
CMD envsubst < /usr/share/nginx/html/assets/env.js > /usr/share/nginx/html/assets/env-temp.js \
    && mv /usr/share/nginx/html/assets/env-temp.js /usr/share/nginx/html/assets/env.js \
    && nginx -g 'daemon off;'
```

### 🚀 Use in Angular
#### Step 1: Create `config.service.ts`
```ts
import { Injectable } from '@angular/core';

// Tell TypeScript we're using a global window variable
declare const window: any;

@Injectable({
    providedIn: 'root'
})
export class ConfigService {
    get apiUrl(): string {
        return window.__env?.API_URL || 'http://localhost:3000';
    }
}

```

#### Step 2: Use in Component (e.g., `upload-file.component.ts`)
```ts
xport class UploadFileComponent implements OnInit{
    constructor(private config: ConfigService) {}

    ngOnInit() {
        console.log('API URL from config service:', this.config.apiUrl);
    }
```
This lets you log and use the environment URL in your logic without recompiling the app.

### 💲 Run With Env

- Start Docker Desktop (for local setup on Windows)

- build first as previously shown

```bash
docker run --rm -p 8080:80 -e API_URL=https://api.example.com my-angular-app
```

Open DevTools > Console:
```
API URL from config service: https://api.example.com
```

---


## Folder Structure
```
frontend/
├── src/
├── app/
│   └── config.service.ts
├── assets/
│   └── env.template.js
├── Dockerfile
├── nginx.conf
└── README.md
```

---

## 🏦 Full Build & Test Summary

- Start Docker Desktop (for local setup on Windows)

```bash
# Build
cd BitAndBeam/frontend
docker build -t my-angular-app .

# Run
PORT=8080
API_URL=https://api.example.com

docker run --rm -p $PORT:80 -e API_URL=$API_URL my-angular-app

 # sometimes use this to removed cached builds to be sure 
docker build --no-cache -t my-angular-app .

```


---

## 🎓 Notes for New Developers
- Make sure Docker is running.
- Use DevTools > Console to check config is injected.
- Always map container port 80 to any free local port.
- Use `docker ps` and `docker stop <id>` to manage containers.

