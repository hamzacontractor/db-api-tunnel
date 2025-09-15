@echo off
REM build-and-push.bat - Build and push DatabaseRag.Api to Azure Container Registry

setlocal enabledelayedexpansion

echo 🚀 Build and Push DatabaseRag.Api to Azure Container Registry
echo ==========================================================

REM Configuration - Update these values for your environment
set ACR_NAME=youracrname
set IMAGE_NAME=db-rag-api
if "%1"=="" (
    set TAG=latest
) else (
    set TAG=%1
)
set FULL_NAME=%ACR_NAME%.azurecr.io/%IMAGE_NAME%:%TAG%

echo 📋 Configuration:
echo    ACR Name: %ACR_NAME%
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

REM Check if Azure CLI is installed
az --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Error: Azure CLI is not installed. Please install Azure CLI and try again.
    echo    Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
    exit /b 1
)

echo 🔨 Building container...

REM Change to src directory for proper context
cd ..\src

REM Build the container
docker build -f ..\.docker\Dockerfile -t "%FULL_NAME%" .
if errorlevel 1 (
    echo ❌ Failed to build container
    cd ..\.docker
    exit /b 1
)

cd ..\.docker

echo ✅ Container built successfully!

echo 🔐 Logging into Azure Container Registry...
az acr login --name %ACR_NAME%
if errorlevel 1 (
    echo ❌ Failed to login to ACR. Please check your Azure authentication.
    exit /b 1
)

echo 📤 Pushing image to ACR...
docker push "%FULL_NAME%"
if errorlevel 1 (
    echo ❌ Failed to push image to ACR
    exit /b 1
)

echo ✅ Image pushed successfully!

REM Verify the push
echo 🔍 Verifying image in ACR...
az acr repository show-tags --name %ACR_NAME% --repository %IMAGE_NAME% --output table
if errorlevel 1 (
    echo ⚠️  Could not verify image tags, but push appeared successful
) else (
    echo ✅ Image verified in ACR!
)

echo.
echo 🎉 Build and push completed successfully!
echo    Image: %FULL_NAME%
echo    Ready for deployment to Azure services
echo.

endlocal