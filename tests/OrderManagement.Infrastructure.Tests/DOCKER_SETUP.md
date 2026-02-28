# 🐳 Docker Setup Guide for Integration Tests

## Quick Start

### Step 1: Install Docker Desktop
1. Download from: https://www.docker.com/products/docker-desktop/
2. Install and restart your computer if prompted

### Step 2: Start Docker Desktop
1. Launch "Docker Desktop" application
2. Wait until you see "Docker Desktop is running" in the system tray
3. Verify with PowerShell:
   ```powershell
   docker --version
   ```
   Should output something like: `Docker version 24.0.x`

### Step 3: Run Your Tests
```powershell
# From solution root
dotnet test tests\OrderManagement.Infrastructure.Tests\OrderManagement.Infrastructure.Tests.csproj
```

## ✅ Verify Docker is Working

```powershell
# Check Docker is running
docker info

# Pull MSSQL image (optional - tests will do this automatically)
docker pull mcr.microsoft.com/mssql/server:2022-latest
```

## 🔍 Monitoring Test Containers

While tests are running:
```powershell
# List running containers
docker ps

# View container logs
docker logs <container-id>
```

## 🧹 Cleanup (if needed)

Testcontainers automatically cleans up, but if containers persist:
```powershell
# List all containers
docker ps -a

# Remove specific container
docker rm -f <container-id>

# Remove all stopped containers
docker container prune
```

## ⚠️ Common Issues

### "Docker daemon is not running"
**Solution**: Start Docker Desktop application

### "Cannot connect to Docker daemon"
**Solution**:
1. Restart Docker Desktop
2. Check Windows Services → "Docker Desktop Service" is running

### Slow first test run
**Expected**: First run downloads MSSQL image (~1.5 GB). Subsequent runs are fast.

### Port conflicts
Testcontainers uses random ports, so this should not happen. If it does, restart Docker Desktop.

## 📊 System Requirements

- **RAM**: 4 GB minimum (8 GB recommended)
- **Disk**: 2 GB free space for Docker images
- **OS**: Windows 10/11 Pro, Enterprise, or Education with WSL2

## 🎯 Next Steps

Once Docker is running:
1. Open Test Explorer in Visual Studio
2. Run `ArticleCommandRepositoryTests`
3. All tests should pass ✅
