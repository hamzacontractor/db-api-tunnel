# DatabaseRag API

A high-performance .NET 8 REST API service providing secure database access and query capabilities for SQL Server and Cosmos DB databases. This API serves as a database tunnel service, enabling safe database operations through a controlled interface.

## 🚀 Features

- **Multi-Database Support**: SQL Server and Cosmos DB integration
- **Secure Connection Management**: Connection strings passed via headers
- **Comprehensive API**: Schema discovery, connection testing, and query execution
- **Health Monitoring**: Built-in health checks and status monitoring
- **Production Ready**: Docker containerization with security best practices
- **Comprehensive Testing**: Full test suite with integration and unit tests
- **API Documentation**: Swagger/OpenAPI integration for interactive documentation

## 📁 Project Structure

```
db-api-tunnel/
├── src/
│   ├── DatabaseRag.Api/              # Main API project
│   │   ├── Controllers/              # API endpoint controllers
│   │   ├── Services/                 # Business logic services
│   │   ├── Models/                   # Request/response models
│   │   └── Program.cs                # Application entry point
│   └── DatabaseRag.Api.Tests/        # Comprehensive test suite
│       ├── Controllers/              # Controller tests
│       ├── Services/                 # Service tests
│       ├── Integration/              # Integration tests
│       └── appsettings.Test.json     # Test configuration
├── .docker/
│   ├── Dockerfile                    # Multi-stage Docker build
│   └── Dockerfile.runtime            # Runtime-only Docker image
├── docker-compose.yml               # Docker Compose configuration
└── README.md                        # This documentation
```

## 🛠️ Technology Stack

- **.NET 8**: Latest LTS framework for high performance
- **ASP.NET Core**: Web API with minimal API pattern
- **Swagger/OpenAPI**: Interactive API documentation
- **SQL Server**: Enterprise database support
- **Cosmos DB**: NoSQL document database support
- **Docker**: Containerization for consistent deployment
- **xUnit**: Testing framework with comprehensive coverage

## 🔧 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (for containerized deployment)
- SQL Server or Cosmos DB instance (for testing)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd db-api-tunnel
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run tests**
   ```bash
   dotnet test src/DatabaseRag.Api.Tests/
   ```

4. **Start the API**
   ```bash
   dotnet run --project src/DatabaseRag.Api
   ```

5. **Access Swagger UI**
   Open browser to `https://localhost:7108/swagger` (or configured port)

### Docker Deployment

1. **Build and run with Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Run tests in Docker**
   ```bash
   docker-compose --profile testing up databaserag-tests
   ```

3. **Access the API**
   - API: `http://localhost:8080`
   - Health Check: `http://localhost:8080/health`
   - Swagger: `http://localhost:8080/swagger`

## 📋 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` |
| `ASPNETCORE_URLS` | Server binding URLs | `http://+:8080` |
| `DOTNET_USE_POLLING_FILE_WATCHER` | File change monitoring | `true` |
| `DOTNET_RUNNING_IN_CONTAINER` | Container detection | `true` |

### Test Configuration

The test project includes `appsettings.Test.json` with real database connection examples:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=TestDB;Integrated Security=true;",
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=your-key;"
  },
  "TestSettings": {
    "DatabaseName": "TestDatabase",
    "ContainerName": "TestContainer"
  }
}
```

## 🔌 API Endpoints

All endpoints require a `ConnectionString` header for database authentication. The API provides comprehensive database operations with structured responses and error handling.

### Health Check

#### `GET /health`
Performs a health check on the service.

**Response (200)**
```json
{
  "status": "Healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "version": "1.0.0"
}
```

---

### SQL Server Endpoints

All SQL Server endpoints are under the `/api/sql` path and require the `ConnectionString` header.

#### `POST /api/sql/schema`
Retrieves SQL Server database schema information.

**Headers**
- `ConnectionString`: SQL Server connection string

**Response (200)**
```json
{
  "success": true,
  "schema": {
    "tables": [
      {
        "name": "Users",
        "schema": "dbo",
        "columns": [
          {
            "name": "Id",
            "dataType": "int",
            "isNullable": false,
            "isPrimaryKey": true
          },
          {
            "name": "Username",
            "dataType": "nvarchar",
            "maxLength": 50,
            "isNullable": false,
            "isPrimaryKey": false
          }
        ],
        "indexes": [
          {
            "name": "PK_Users",
            "type": "PrimaryKey",
            "columns": ["Id"]
          }
        ]
      }
    ],
    "views": [],
    "procedures": []
  },
  "error": null
}
```

**Error Response (400)**
```json
{
  "success": false,
  "schema": null,
  "error": "Connection failed: Invalid connection string"
}
```

#### `POST /api/sql/test`
Tests SQL Server database connection.

**Headers**
- `ConnectionString`: SQL Server connection string

**Response (200)**
```json
{
  "success": true,
  "message": "Connection successful"
}
```

**Error Response (400)**
```json
{
  "success": false,
  "error": "Connection failed: Server not found"
}
```

#### `POST /api/sql/query`
Executes a SQL query against the database.

**Headers**
- `ConnectionString`: SQL Server connection string

**Request Body**
```json
{
  "query": "SELECT TOP 10 * FROM Users WHERE IsActive = 1"
}
```

**Response (200)**
```json
{
  "success": true,
  "data": [
    {
      "Id": 1,
      "Username": "john_doe",
      "Email": "john@example.com",
      "IsActive": true
    },
    {
      "Id": 2,
      "Username": "jane_smith",
      "Email": "jane@example.com",
      "IsActive": true
    }
  ],
  "analyticalDetails": {
    "rowCount": 2,
    "columnCount": 4,
    "executionTimeMs": 45,
    "query": "SELECT TOP 10 * FROM Users WHERE IsActive = 1",
    "timestamp": "2024-01-15T10:30:00Z",
    "columns": [
      {
        "name": "Id",
        "dataType": "int",
        "isNullable": false
      },
      {
        "name": "Username",
        "dataType": "nvarchar",
        "isNullable": false
      }
    ]
  },
  "error": null
}
```

**Error Response (400)**
```json
{
  "success": false,
  "data": null,
  "analyticalDetails": {
    "rowCount": 0,
    "columnCount": 0,
    "executionTimeMs": 0,
    "query": "SELECT * FROM InvalidTable",
    "timestamp": "2024-01-15T10:30:00Z",
    "columns": []
  },
  "error": "Invalid object name 'InvalidTable'"
}
```

---

### Cosmos DB Endpoints

All Cosmos DB endpoints are under the `/api/cosmos` path and require the `ConnectionString` header.

#### `POST /api/cosmos/schema`
Retrieves Cosmos DB database schema information.

**Headers**
- `ConnectionString`: Cosmos DB connection string

**Request Body**
```json
{
  "databaseName": "MyDatabase"
}
```

**Response (200)**
```json
{
  "success": true,
  "schema": {
    "databaseName": "MyDatabase",
    "containers": [
      {
        "name": "Users",
        "partitionKeyPath": "/userId",
        "properties": [
          {
            "name": "id",
            "type": "string",
            "isRequired": true
          },
          {
            "name": "userId",
            "type": "string",
            "isRequired": true
          },
          {
            "name": "email",
            "type": "string",
            "isRequired": false
          }
        ]
      }
    ]
  },
  "error": null
}
```

#### `POST /api/cosmos/test`
Tests Cosmos DB database connection.

**Headers**
- `ConnectionString`: Cosmos DB connection string

**Request Body**
```json
{
  "databaseName": "MyDatabase"
}
```

**Response (200)**
```json
{
  "success": true,
  "message": "Connection successful"
}
```

#### `POST /api/cosmos/query`
Executes a SQL query against Cosmos DB.

**Headers**
- `ConnectionString`: Cosmos DB connection string

**Request Body**
```json
{
  "query": "SELECT * FROM c WHERE c.isActive = true",
  "databaseName": "MyDatabase",
  "containerName": "Users"
}
```

**Response (200)**
```json
{
  "success": true,
  "data": [
    {
      "id": "1",
      "userId": "user_123",
      "email": "user@example.com",
      "isActive": true,
      "_rid": "document-rid",
      "_self": "dbs/db/colls/coll/docs/doc/",
      "_etag": "\"0000d986-0000-0000-0000-000000000000\""
    }
  ],
  "analyticalDetails": {
    "itemCount": 1,
    "executionTimeMs": 125,
    "requestCharge": 2.95,
    "query": "SELECT * FROM c WHERE c.isActive = true",
    "databaseName": "MyDatabase",
    "containerName": "Users",
    "timestamp": "2024-01-15T10:30:00Z"
  },
  "error": null
}
```

## 🚨 Error Handling

The API uses consistent error response formats across all endpoints:

### Common Error Codes

| Status Code | Description |
|-------------|-------------|
| `200` | Success |
| `400` | Bad Request - Invalid input or database error |
| `500` | Internal Server Error - Unexpected server error |

### Error Response Format

```json
{
  "success": false,
  "error": "Detailed error message",
  "data": null,
  "analyticalDetails": {
    // Context-specific analytical information
  }
}
```

### Common Error Scenarios

1. **Missing Connection String**
   ```json
   {
     "error": "ConnectionString header is required"
   }
   ```

2. **Invalid Connection String**
   ```json
   {
     "error": "Connection failed: Invalid connection string format"
   }
   ```

3. **Database Connection Failed**
   ```json
   {
     "error": "Connection failed: Server not found or access denied"
   }
   ```

4. **Invalid Query**
   ```json
   {
     "error": "SQL syntax error: Invalid object name 'TableName'"
   }
   ```

## 🔒 Security Considerations

- **Connection Strings**: Passed via headers to avoid logging in URLs
- **Input Validation**: All inputs are validated and sanitized
- **Error Messages**: Sanitized to prevent information leakage
- **Container Security**: Non-root user execution in Docker containers
- **Network Security**: No direct database access - API acts as secure proxy

## 📊 Monitoring and Health

- **Health Endpoint**: `/health` provides service status
- **Request Tracing**: Analytical details in all query responses
- **Error Logging**: Comprehensive error logging for troubleshooting
- **Performance Metrics**: Execution time and resource usage tracking

## 🐳 Docker Usage

### Building the Application

```bash
# Build the main API image
docker build -f .docker/Dockerfile -t databaserag-api .

# Build and run tests
docker build -f .docker/Dockerfile --target test -t databaserag-tests .

# Build development image with all tools
docker build -f .docker/Dockerfile --target build -t databaserag-dev .
```

### Running with Docker Compose

```bash
# Start the API service
docker-compose up -d

# Start with testing profile
docker-compose --profile testing up

# Start with development profile
docker-compose --profile development up

# View logs
docker-compose logs -f databaserag-api

# Stop all services
docker-compose down
```

### Docker Commands Reference

```bash
# Build and run API only
docker-compose up --build databaserag-api

# Run tests in Docker
docker-compose up --build databaserag-tests

# Scale API instances
docker-compose up --scale databaserag-api=3

# Run in detached mode
docker-compose up -d

# Force recreate containers
docker-compose up --force-recreate

# Remove all containers and volumes
docker-compose down -v
```

### Environment Configuration

Create a `.env` file for environment-specific configuration:

```bash
# .env file
ASPNETCORE_ENVIRONMENT=Development
API_PORT=8080
SQL_CONNECTION_STRING=Server=localhost;Database=MyDB;Integrated Security=true;
COSMOS_CONNECTION_STRING=AccountEndpoint=https://localhost:8081/;AccountKey=your-key;
```

Then use it with Docker Compose:

```bash
docker-compose --env-file .env up
```

### Docker Health Checks

The containers include health checks that can be monitored:

```bash
# Check container health
docker inspect --format='{{.State.Health.Status}}' databaserag-api

# View health check logs
docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' databaserag-api
```

### Troubleshooting Docker Issues

1. **Container won't start**
   ```bash
   # Check logs
   docker-compose logs databaserag-api
   
   # Check container status
   docker ps -a
   ```

2. **Port conflicts**
   ```bash
   # Change port in docker-compose.yml
   ports:
     - "8081:8080"  # Use different host port
   ```

3. **Database connection issues**
   ```bash
   # Test connection from container
   docker exec -it databaserag-api curl http://localhost:8080/health
   ```

4. **Clean rebuild**
   ```bash
   # Remove all containers and rebuild
   docker-compose down
   docker system prune -f
   docker-compose up --build
   ```

## 🧪 Testing

### Running Tests Locally

```bash
# Run all tests
dotnet test src/DatabaseRag.Api.Tests/

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=Integration
```

### Running Tests in Docker

```bash
# Run tests with Docker Compose
docker-compose --profile testing up databaserag-tests

# Run tests and get results
docker-compose up databaserag-tests
docker cp databaserag-tests:/app/test-results ./test-results
```

### Test Configuration

Update `src/DatabaseRag.Api.Tests/appsettings.Test.json` with your test database connections:

```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=TestDB;Integrated Security=true;TrustServerCertificate=true;",
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;"
  },
  "TestSettings": {
    "DatabaseName": "TestDatabase",
    "ContainerName": "TestContainer",
    "TestTimeout": 30000
  }
}
```

## 🚀 Deployment

### Production Deployment

1. **Build production image**
   ```bash
   docker build -f .docker/Dockerfile -t databaserag-api:latest .
   ```

2. **Run with production settings**
   ```bash
   docker run -d \
     --name databaserag-api \
     -p 8080:8080 \
     -e ASPNETCORE_ENVIRONMENT=Production \
     databaserag-api:latest
   ```

### CI/CD Pipeline Example

```yaml
# GitHub Actions example
name: Build and Deploy
on:
  push:
    branches: [main]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Run tests
        run: docker build --target test -t test-image .
        
  build:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build image
        run: docker build -t databaserag-api:${{ github.sha }} .
      - name: Deploy
        run: echo "Deploy to your container registry"
```

## 📚 Additional Resources

- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [ASP.NET Core Web API](https://docs.microsoft.com/en-us/aspnet/core/web-api/)
- [Docker Documentation](https://docs.docker.com/)
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/sql-server/)
- [Cosmos DB Documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.