# Database.Api.Tunnel Docker Setup and Azure Container Registry Deployment

This directory contains the Docker configuration for containerizing the Database.Api.Tunnel application and deploying it to Azure Container Registry (ACR).

## 📁 Files Overview

- `Dockerfile` - Multi-stage Dockerfile for building the .NET API from src/ folder
- `Dockerfile.runtime` - Simple runtime-only Dockerfile (fallback)
- `docker-compose.yml` - Docker Compose configuration for local development  
- `.dockerignore` - Files to exclude from Docker build context
- `build-container.bat` - Windows batch script for building containers
- `build-container.sh` - Linux/Mac shell script for building containers
- `build-and-push.bat` - Script to build and push to Azure Container Registry

## 🐳 Prerequisites

1. **Docker Desktop** - Install from https://www.docker.com/products/docker-desktop
2. **Azure CLI** - Install from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
3. **Azure Subscription** - With permissions to create/manage Container Registry

## 🚀 Quick Start

### 1. Configure Azure Container Registry

First, create an Azure Container Registry (if you don't have one):

```bash
# Login to Azure
az login

# Create a resource group (if needed)
az group create --name myResourceGroup --location eastus

# Create Azure Container Registry
az acr create --resource-group myResourceGroup --name youracrname --sku Basic

# Note: Update the ACR_NAME in build-and-push.bat with your registry name
```

### 2. Build and Push to ACR

Run the automated script:

```cmd
# From the .docker directory
build-and-push.bat
```

Or manually:

```cmd
# Build the image (from .docker directory)
cd ../src
docker build -f ../.docker/Dockerfile -t db-rag-api:v1.0 .

# Login to ACR
az acr login --name youracrname

# Tag the image
docker tag db-rag-api:v1.0 youracrname.azurecr.io/db-rag-api:v1.0

# Push to ACR
docker push youracrname.azurecr.io/db-rag-api:v1.0
```

### 3. Verify Deployment

Check that the image was pushed successfully:

```bash
az acr repository show-tags --name youracrname --repository db-rag-api --output table
```

## 🏗️ Docker Configuration Details

### Multi-Stage Build Process

The Dockerfile uses a multi-stage build for optimization with the correct src/ folder structure:

1. **Build Stage** - Restores dependencies and compiles the application from src/ folder
2. **Test Stage** - Runs unit tests (optional, can be skipped)  
3. **Publish Stage** - Creates optimized publish package
4. **Runtime Stage** - Final runtime image with ASP.NET Core runtime

### Security Features

- Non-root user execution
- Minimal runtime image (ASP.NET Core runtime only)
- Health checks configured
- Proper file permissions

### Exposed Ports

- **8080** - Main API port (configured for container environments)

### Build Context

The Docker build context is set to the `../src` directory, which contains:
- `Database.Api.Tunnel/` - Main API project
- `Database.Api.Tunnel.Tests/` - Test project

### Updated Dockerfile Structure

The Dockerfile has been refactored to work with the new project structure:

```dockerfile
# Dockerfile paths are now relative to src/ folder:
COPY Database.Api.Tunnel/Database.Api.Tunnel.csproj Database.Api.Tunnel/
COPY Database.Api.Tunnel.Tests/Database.Api.Tunnel.Tests.csproj Database.Api.Tunnel.Tests/

# Restore, build, and publish commands updated accordingly:
RUN dotnet restore Database.Api.Tunnel/Database.Api.Tunnel.csproj
RUN dotnet build Database.Api.Tunnel/Database.Api.Tunnel.csproj -c Release --no-restore
RUN dotnet publish Database.Api.Tunnel/Database.Api.Tunnel.csproj -c Release -o /app/publish
```

### Docker Compose Configuration

The docker-compose.yml has been updated with correct build context:

```yaml
services:
  database-api-tunnel:
    build:
      context: ../src              # Build from src/ directory
      dockerfile: ../.docker/Dockerfile  # Use Dockerfile from .docker/
      target: runtime
```

## 🔧 Configuration

### Environment Variables

The container supports these environment variables:

- `ASPNETCORE_ENVIRONMENT` - Set to `Production` for production deployments
- `ASPNETCORE_URLS` - HTTP URLs to bind to (default: `http://+:8080`)
- `DOTNET_USE_POLLING_FILE_WATCHER` - File watcher configuration
- `DOTNET_RUNNING_IN_CONTAINER` - Container detection flag

### Database Configuration

The API requires database connection strings. Configure these through:

1. **Environment Variables** (recommended for containers)
2. **Azure App Configuration**
3. **Azure Key Vault** (for secrets)

## 📦 Usage Examples

### Building with Updated Scripts

The build scripts have been updated to work with the new src/ folder structure:

```bash
# Windows (from .docker directory)
build-container.bat latest

# Linux/Mac (from .docker directory)  
./build-container.sh latest

# Docker Compose (from .docker directory)
docker-compose up -d
docker-compose --profile testing up    # Run tests
docker-compose --profile development up # Development mode
```

### Manual Build Process

```bash
# From .docker directory
cd ../src

# Build main image
docker build -f ../.docker/Dockerfile -t db-rag-api:latest .

# Build and run tests only
docker build -f ../.docker/Dockerfile --target test -t db-rag-api-tests .

# Build development image
docker build -f ../.docker/Dockerfile --target build -t db-rag-api-dev .

# Return to .docker directory
cd ../.docker
```

### Run Locally

```bash
# Pull from ACR
docker pull youracrname.azurecr.io/db-rag-api:v1.0

# Run locally
docker run -d -p 8080:8080 --name db-rag-api youracrname.azurecr.io/db-rag-api:v1.0

# Check health
curl http://localhost:8080/health
```

### Deploy to Azure

#### Azure Container Instances (ACI)

```bash
az container create \
  --resource-group myResourceGroup \
  --name db-rag-api \
  --image youracrname.azurecr.io/db-rag-api:v1.0 \
  --cpu 1 \
  --memory 1 \
  --registry-login-server youracrname.azurecr.io \
  --registry-username youracrname \
  --registry-password $(az acr credential show --name youracrname --query passwords[0].value -o tsv) \
  --ports 80 \
  --environment-variables ASPNETCORE_URLS=http://+:80 \
  --ip-address public
```

#### Azure App Service

1. Create an App Service with Docker support
2. Configure the container image: `youracrname.azurecr.io/db-rag-api:v1.0`
3. Set environment variables in App Service configuration

#### Azure Kubernetes Service (AKS)

Use the image in your Kubernetes deployments:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: db-rag-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: db-rag-api
  template:
    metadata:
      labels:
        app: db-rag-api
    spec:
      containers:
      - name: db-rag-api
        image: youracrname.azurecr.io/db-rag-api:v1.0
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## 🔍 Troubleshooting

### Common Issues

1. **Docker build fails**
   - Ensure Docker Desktop is running
   - Check that all project files are present
   - Verify .NET SDK version compatibility

2. **ACR login fails**
   - Run `az login` to authenticate
   - Check that you have permissions on the ACR
   - Verify ACR name is correct

3. **Push fails**
   - Ensure you're logged into ACR
   - Check network connectivity
   - Verify ACR has sufficient storage quota

4. **Container won't start**
   - Check logs: `docker logs <container-id>`
   - Verify environment variables
   - Ensure required ports are available

### Health Checks

The container includes health checks. Monitor the `/health` endpoint:

```bash
# Check health
curl http://localhost:8080/health

# Expected response: Healthy
```

## 📊 Monitoring and Logging

### Container Logs

```bash
# View container logs
docker logs db-rag-api

# Follow logs in real-time
docker logs -f db-rag-api
```

### Azure Monitor Integration

For production deployments, integrate with Azure Monitor:

1. Enable Azure Monitor for containers
2. Configure log analytics workspace
3. Set up alerts and dashboards

## 🔒 Security Considerations

1. **Image Scanning** - Use Azure Security Center to scan for vulnerabilities
2. **Secrets Management** - Use Azure Key Vault for sensitive configuration
3. **Network Security** - Configure proper network security groups
4. **Access Control** - Use Azure RBAC for ACR access management

## 📚 Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/)
- [Azure CLI Documentation](https://docs.microsoft.com/en-us/cli/azure/)
- [ASP.NET Core Docker](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)

## 🤝 Contributing

When making changes to the Docker configuration:

1. Test builds locally
2. Update documentation
3. Ensure security best practices
4. Test deployment scenarios