# Multi-stage Dockerfile for DatabaseRag.Api with testing
# Stage 1: Build and test stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files first to leverage Docker layer caching
COPY DatabaseRag.Api/DatabaseRag.Api.csproj DatabaseRag.Api/
COPY DatabaseRag.Api.Tests/DatabaseRag.Api.Tests.csproj DatabaseRag.Api.Tests/

# Restore dependencies for both projects (separate layer for better caching)
RUN dotnet restore DatabaseRag.Api/DatabaseRag.Api.csproj
RUN dotnet restore DatabaseRag.Api.Tests/DatabaseRag.Api.Tests.csproj

# Copy source code for both projects
COPY DatabaseRag.Api/ DatabaseRag.Api/
COPY DatabaseRag.Api.Tests/ DatabaseRag.Api.Tests/

# Build both projects
RUN dotnet build DatabaseRag.Api/DatabaseRag.Api.csproj -c Release --no-restore
RUN dotnet build DatabaseRag.Api.Tests/DatabaseRag.Api.Tests.csproj -c Release --no-restore

# Stage 2: Test stage (optional - can be skipped in production builds)
FROM build AS test
WORKDIR /app

# Run tests (this stage can be targeted specifically for CI/CD)
RUN dotnet test DatabaseRag.Api.Tests/DatabaseRag.Api.Tests.csproj \
    -c Release \
    --no-build \
    --no-restore \
    --logger trx \
    --results-directory /app/test-results

# Stage 3: Publish stage
FROM build AS publish
WORKDIR /app

# Publish the API in Release mode
RUN dotnet publish DatabaseRag.Api/DatabaseRag.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# Stage 4: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port 8080 (standard container port)
EXPOSE 8080

# Configure ASP.NET Core for container environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Add health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD dotnet --info > /dev/null || exit 1

# Set the entry point
ENTRYPOINT ["dotnet", "DatabaseRag.Api.dll"]