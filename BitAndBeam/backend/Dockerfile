# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copy csproj and restore
COPY ./BUILD.ING/*.csproj ./BUILD.ING/
WORKDIR /app/BUILD.ING
RUN dotnet restore
RUN dotnet add package Npgsql
RUN dotnet add package Npgsql.NetTopologySuite

# Copy rest of the source code
COPY ./BUILD.ING/. ./
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install dos2unix to normalize scripts
RUN apt-get update && apt-get install -y dos2unix && rm -rf /var/lib/apt/lists/*

# To allow configurable ports via environment variable
ENV ASPNETCORE_URLS=http://+:5000

# Copy and normalize wait-for-it.sh
COPY wait-for-it.sh wait-for-it.sh
RUN dos2unix wait-for-it.sh && chmod +x wait-for-it.sh

# Copy build artifacts from previous stage
COPY --from=build /app/BUILD.ING/out ./
# Copy resources directory for document_categories.json
COPY ./resources ./resources

# Expose port
EXPOSE 5000
#EXPOSE 5001

# Wait for PostgreSQL then start app
ENTRYPOINT ["./wait-for-it.sh", "postgres:5432", "--", "dotnet", "Build.ING.dll"]
