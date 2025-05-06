# 🐳 Docker Guide for Building Document Management API

This guide helps you run the **ASP.NET Core Web API** using Docker.
---

## 🧰 Prerequisites

Make sure Docker is installed and running on your system.

### 🔗 Install Docker

- **Windows/macOS**: [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)
- **Linux (Ubuntu/Debian)**:

  sudo apt update
  sudo apt install docker.io
  sudo systemctl start docker
  sudo systemctl enable docker

NB:🔁 You may need to restart your system or log out/in after installing Docker

### 🛠️ Build the Docker Image
```bash
docker build -t building-backend .
```

        -t building-backend assigns a name (tag) to the built image.

        . tells Docker to use the current directory for the build context.

### ▶️ Run the Docker Container
```bash
docker run -d -p 5000:5000 --name building-api building-backend
```

        -d: Run in detached mode

        -p 5000:5000: Maps your machine's port 5000 to the container's port

        --name building-api: Optional name for the container

### 🌐 Access the API

Once running, open your browser or use curl to access:

`http://localhost:5000`	Optional root message

`http://localhost:5000/weatherforecast`	Sample data endpoint

`http://localhost:5000/healthz`	Health check (200 OK)

`http://localhost:5000/swagger`	Swagger UI (only in development environment)

### 🧼 Stop and Remove the Container
```bash
docker stop building-api
    
docker rm building-api
```
