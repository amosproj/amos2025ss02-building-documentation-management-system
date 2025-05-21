# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published app and wait script
COPY --from=build /app/out ./
COPY wait-for-it.sh ./  # Make sure path matches your actual project structure

RUN chmod +x wait-for-it.sh

EXPOSE 5000

# Wait for PostgreSQL before starting the app
ENTRYPOINT ["./wait-for-it.sh", "postgres:5432", "--", "dotnet", "Build.ING.dll"]

