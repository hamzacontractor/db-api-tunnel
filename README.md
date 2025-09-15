# Database API Tunnel

A high-performance .NET 8 REST API service providing secure database access and query capabilities for SQL Server and Cosmos DB databases. This project serves as a database tunnel service, enabling safe database operations through a controlled, containerized interface.

## 🚀 Quick Start

### Local Development
```bash
# Build and run the API
cd src/Database.Api.Tunnel
dotnet run

# Run tests
cd ../Database.Api.Tunnel.Tests
dotnet test
```

### Docker Deployment
```bash
# Build and run with Docker
cd .docker
docker-compose up -d

# Access the API
# API: http://localhost:8080
# Swagger: http://localhost:8080/swagger
# Health: http://localhost:8080/health
```

## 📁 Project Structure

```
db-api-tunnel/
├── src/                              # Source code
│   ├── Database.Api.Tunnel/          # Main API project
│   │   ├── Controllers/              # API endpoints (Minimal API style)
│   │   ├── Services/                 # Business logic implementations
│   │   ├── Models/                   # Request/response DTOs and schema models
│   │   ├── Extensions/               # DI configuration and extensions
│   │   ├── Utilities/                # Helper classes and analyzers
│   │   └── README.md                 # Complete API documentation
│   └── Database.Api.Tunnel.Tests/    # Comprehensive test suite
│       ├── Controllers/              # Controller tests
│       ├── Services/                 # Service tests
│       ├── Integration/              # Integration tests
│       └── Utilities/                # Utility tests
├── .docker/                          # Docker configuration
│   ├── Dockerfile                    # Multi-stage Docker build
│   ├── Dockerfile.runtime            # Runtime-only Docker image
│   ├── docker-compose.yml           # Docker Compose configuration
│   ├── build-container.bat          # Windows build script
│   ├── build-container.sh           # Linux/Mac build script
│   ├── build-and-push.bat           # Azure Container Registry script
│   └── README.md                     # Docker documentation
└── README.md                         # This file - project overview
```

## 🛠️ Technology Stack

- **.NET 8** - Latest LTS framework for high performance
- **ASP.NET Core** - Minimal API pattern for lightweight endpoints
- **SQL Server** - Enterprise database support via Microsoft.Data.SqlClient
- **Cosmos DB** - NoSQL document database via Microsoft.Azure.Cosmos
- **Docker** - Multi-stage containerization with security best practices
- **Swagger/OpenAPI** - Interactive API documentation
- **xUnit** - Comprehensive testing framework

## 🔌 Key Features

- **Multi-Database Support**: SQL Server and Cosmos DB integration
- **Secure Connection Management**: Connection strings passed via headers
- **Clean Architecture**: SOLID principles, CQRS patterns, and DDD
- **Production Ready**: Docker containerization with health checks
- **Comprehensive Testing**: Full test suite with integration tests
- **Performance Optimized**: Source-generated JSON serialization
- **Error Handling**: Consistent error responses across all endpoints
- **Health Monitoring**: Built-in health checks and status monitoring

## 🔌 API Endpoints

### Health Check
- `GET /health` - Service health status

### SQL Server
- `POST /api/sql/test` - Test database connection
- `POST /api/sql/schema` - Retrieve database schema
- `POST /api/sql/query` - Execute SQL queries

### Cosmos DB
- `POST /api/cosmos/test` - Test database connection
- `POST /api/cosmos/schema` - Retrieve container schemas
- `POST /api/cosmos/query` - Execute SQL queries against Cosmos DB

> **Authentication**: All endpoints require `ConnectionString` header for database access.

## 🐳 Docker Usage

### Quick Start with Docker Compose
```bash
cd .docker
docker-compose up -d                    # Start API service
docker-compose --profile testing up    # Run with tests
docker-compose logs -f database-api-tunnel  # View logs
docker-compose down                     # Stop services
```

### Build Custom Images
```bash
cd .docker
./build-container.sh latest           # Linux/Mac
build-container.bat latest            # Windows
```

### Azure Container Registry
```bash
cd .docker
# Update ACR_NAME in build-and-push.bat first
build-and-push.bat v1.0              # Build and push to ACR
```

## 🔒 Security Features

- **Secure Headers**: Connection strings passed via headers (not URLs)
- **Input Validation**: All inputs validated and sanitized
- **Container Security**: Non-root user execution
- **Error Sanitization**: Prevents information leakage
- **Network Isolation**: API acts as secure database proxy

## 📊 Monitoring & Health

- **Health Endpoint**: `/health` provides real-time service status
- **Performance Metrics**: Execution time tracking in all responses
- **Analytical Details**: Query complexity and resource usage
- **Error Logging**: Comprehensive error tracking and categorization
- **Docker Health Checks**: Built-in container health monitoring

## 🚀 Deployment Options

### Local Development
- Direct .NET 8 execution
- Docker Compose for full stack

### Azure Cloud
- **Azure Container Instances** - Simple container hosting
- **Azure App Service** - Web app with container support  
- **Azure Kubernetes Service** - Scalable container orchestration
- **Azure Container Registry** - Private container images

### On-Premises
- Docker containers on any Docker-compatible host
- Kubernetes clusters
- Docker Swarm mode

## 📚 Documentation

- **[Complete API Documentation](src/Database.Api.Tunnel/README.md)** - Detailed API reference, architecture, and examples
- **[Docker Documentation](.docker/README.md)** - Containerization guide and deployment instructions
- **[Test Documentation](src/Database.Api.Tunnel.Tests/)** - Test strategy and execution

## 🔧 Development

### Prerequisites
- .NET 8 SDK
- Docker Desktop (for containers)
- SQL Server or Cosmos DB (for testing)

### Build and Test
```bash
# Build solution
dotnet build src/Database.Api.Tunnel

# Run all tests
dotnet test src/Database.Api.Tunnel.Tests

# Run with hot reload
dotnet watch run --project src/Database.Api.Tunnel
```

### Architecture Principles
- **Clean Architecture** with clear separation of concerns
- **SOLID Principles** applied throughout
- **Minimal API Pattern** for lightweight endpoints
- **Service Layer Pattern** for business logic
- **Repository Pattern** (implicit) for data access
- **Factory Pattern** for service creation

## 🤝 Contributing

1. Follow clean architecture principles
2. Maintain comprehensive test coverage
3. Update documentation for changes
4. Ensure Docker builds work correctly
5. Test security and error handling
6. Validate performance impact

---

For detailed API documentation, examples, and deployment guides, see the **[complete documentation](src/Database.Api.Tunnel/README.md)**.