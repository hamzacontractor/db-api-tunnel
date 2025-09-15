#!/bin/bash

# build-container.sh - Build script for DatabaseRag.Api container

set -e  # Exit on any error

echo "🐳 Building DatabaseRag.Api Container"
echo "====================================="

# Configuration
IMAGE_NAME="db-rag-api"
TAG="${1:-latest}"
FULL_NAME="${IMAGE_NAME}:${TAG}"

echo "📋 Configuration:"
echo "   Image Name: ${IMAGE_NAME}"
echo "   Tag: ${TAG}"
echo "   Full Name: ${FULL_NAME}"
echo ""

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    echo "❌ Error: Docker is not running. Please start Docker and try again."
    exit 1
fi

echo "🔨 Building container..."

# Change to src directory for proper context
cd ../src

# Try multi-stage build first
if docker build -f ../.docker/Dockerfile -t "${FULL_NAME}" .; then
    echo "✅ Container built successfully using multi-stage build!"
    cd ../.docker
else
    echo "⚠️  Multi-stage build failed. Trying pre-built approach..."
    
    # Fallback to pre-built approach
    echo "🔨 Building application locally first..."
    cd DatabaseRag.Api
    dotnet publish -c Release -o ./publish
    cd ..
    
    echo "🔨 Building container with pre-built application..."
    if docker build -f ../.docker/Dockerfile.runtime -t "${FULL_NAME}" .; then
        echo "✅ Container built successfully using pre-built approach!"
        cd ../.docker
    else
        echo "❌ Both build approaches failed. Please check the error messages above."
        cd ../.docker
        exit 1
    fi
fi

# Get image info
IMAGE_SIZE=$(docker images "${FULL_NAME}" --format "table {{.Size}}" | tail -n1)
echo ""
echo "📊 Image Information:"
echo "   Name: ${FULL_NAME}"
echo "   Size: ${IMAGE_SIZE}"
echo "   Created: $(date)"

# Test the container
echo ""
echo "🧪 Testing container..."
CONTAINER_ID=$(docker run -d -p 8080:8080 "${FULL_NAME}" 2>/dev/null)

if [ $? -eq 0 ]; then
    echo "   Container started with ID: ${CONTAINER_ID:0:12}"
    
    # Wait for container to be ready
    echo "   Waiting for container to be ready..."
    sleep 5
    
    # Test health endpoint
    if curl -s http://localhost:8080/health >/dev/null; then
        echo "✅ Health check passed!"
    else
        echo "⚠️  Health check failed, but container is running"
    fi
    
    # Clean up test container
    docker stop "${CONTAINER_ID}" >/dev/null
    docker rm "${CONTAINER_ID}" >/dev/null
    echo "   Test container cleaned up"
else
    echo "❌ Failed to start test container"
fi

echo ""
echo "🎉 Build completed successfully!"
echo ""
echo "🚀 Quick Start Commands:"
echo "   Run container:    docker run -d -p 8080:8080 --name db-rag-api ${FULL_NAME}"
echo "   Check health:     curl http://localhost:8080/health"
echo "   Stop container:   docker stop db-rag-api"
echo "   Remove container: docker rm db-rag-api"
echo ""
echo "📖 For more information, see CONTAINER-QUICKSTART.md"