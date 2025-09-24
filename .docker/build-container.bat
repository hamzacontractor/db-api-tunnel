@echo off
REM build-container.bat - Windows build script for Database.Api.Tunnel container

setlocal enabledelayedexpansion

echo 🐳 Building Database.Api.Tunnel Container
echo =====================================

REM Configuration
set IMAGE_NAME=db-api-tunnel
if "%1"=="" (
    set TAG=latest
) else (
    set TAG=%1
)
set FULL_NAME=%IMAGE_NAME%:%TAG%

echo 📋 Configuration:
echo    Image Name: %IMAGE_NAME%
echo    Tag: %TAG%
echo    Full Name: %FULL_NAME%
echo.

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ❌ Error: Docker is not running. Please start Docker and try again.
    exit /b 1
)

echo 🔨 Building container...

REM Change to src directory for proper context
cd ..\src

REM Try multi-stage build first
docker build -f ..\.docker\Dockerfile -t "%FULL_NAME%" .
if errorlevel 1 (
    echo ⚠️  Multi-stage build failed. Trying pre-built approach...
    
    REM Fallback to pre-built approach
    echo 🔨 Building application locally first...
    cd Database.Api.Tunnel
    dotnet publish -c Release -o .\publish
    if errorlevel 1 (
        echo ❌ Failed to build application locally
        cd ..\..\.docker
        exit /b 1
    )
    cd ..
    
    echo 🔨 Building container with pre-built application...
    docker build -f ..\.docker\Dockerfile.runtime -t "%FULL_NAME%" .
    if errorlevel 1 (
        echo ❌ Both build approaches failed. Please check the error messages above.
        cd ..\.docker
        exit /b 1
    )
    echo ✅ Container built successfully using pre-built approach!
    cd ..\.docker
) else (
    echo ✅ Container built successfully using multi-stage build!
    cd ..\.docker
)

REM Get image info
for /f "tokens=*" %%i in ('docker images "%FULL_NAME%" --format "{{.Size}}"') do set IMAGE_SIZE=%%i

echo.
echo 📊 Image Information:
echo    Name: %FULL_NAME%
echo    Size: %IMAGE_SIZE%
echo    Created: %date% %time%

REM Test the container
echo.
echo 🧪 Testing container...

for /f %%i in ('docker run -d -p 8080:8080 "%FULL_NAME%" 2^>nul') do set CONTAINER_ID=%%i

if not "%CONTAINER_ID%"=="" (
    echo    Container started with ID: %CONTAINER_ID:~0,12%
    
    REM Wait for container to be ready
    echo    Waiting for container to be ready...
    timeout /t 5 /nobreak >nul
    
    REM Test health endpoint
    curl -s http://localhost:8080/health >nul 2>&1
    if errorlevel 1 (
        echo ⚠️  Health check failed, but container is running
    ) else (
        echo ✅ Health check passed!
    )
    
    REM Clean up test container
    docker stop "%CONTAINER_ID%" >nul 2>&1
    docker rm "%CONTAINER_ID%" >nul 2>&1
    echo    Test container cleaned up
) else (
    echo ❌ Failed to start test container
)

echo.
echo 🎉 Build completed successfully!
echo.
echo 🚀 Quick Start Commands:
echo    Run container:    docker run -d -p 8080:8080 --name db-api-tunnel %FULL_NAME%
echo    Check health:     curl http://localhost:8080/health
echo    Stop container:   docker stop db-api-tunnel
echo    Remove container: docker rm db-api-tunnel
echo.
echo 📖 For more information, see CONTAINER-QUICKSTART.md

endlocal