# Stage 1: Build
#This use the official Microsoft .NET 8 SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Sets the working directory inside the container to /app. All subsequent commands will be run from this directory.
WORKDIR /app

# Copy csproj (C# Project file)from the host to the container and restore dependencies
COPY ./BUILD.ING/*.csproj ./BUILD.ING/
WORKDIR /app/BUILD.ING
RUN dotnet restore

# Copy everything else 
COPY ./BUILD.ING/. ./
#Compiles and publishes the application in Release mode
RUN dotnet publish -c Release -o out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

#To allow configurable ports via environment variable
ENV ASPNETCORE_URLS=http://+:5000

# Kopiere die Datei ins Image
COPY wait-for-it.sh wait-for-it.sh

# Stelle sicher, dass sie ausführbar ist (innerhalb des Linux-Containers!)
RUN chmod +x wait-for-it.sh

# Starte das Backend erst, wenn PostgreSQL bereit ist
ENTRYPOINT ["./wait-for-it.sh", "postgres:5432", "--", "dotnet", "Build.ING.dll"]




# Copy build artifacts from previous stage
COPY --from=build /app/BUILD.ING/out ./

# Expose port
EXPOSE 5000

# Run the application
ENTRYPOINT ["dotnet", "Build.ING.dll"]
