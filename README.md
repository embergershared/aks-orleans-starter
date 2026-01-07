# AKS Orleans Starter

This repository demonstrates how to **containerize Orleans .NET applications** and **deploy them to Azure Kubernetes Service (AKS)**. It contains two sample applications that showcase different Orleans patterns and deployment strategies.

## 📋 Overview

[Microsoft Orleans](https://github.com/dotnet/orleans) is a framework for building distributed applications in .NET. This repository provides practical examples of:

- Building Orleans applications with ASP.NET Core
- Containerizing Orleans applications using Docker
- Configuring Orleans for Kubernetes clustering
- Deploying Orleans applications to AKS with proper RBAC, services, and networking

## 📁 Repository Structure

```text
├── src/
│   ├── orleans-url-shortener/     # Simple URL shortener Orleans application
│   │   ├── web/                   # ASP.NET Core + Orleans web application
│   │   │   ├── Program.cs         # Application entry point with Orleans configuration
│   │   │   ├── Dockerfile         # Multi-stage Docker build
│   │   │   ├── Grains/            # Orleans grain implementations
│   │   │   └── Models/            # Data models
│   │   └── k8s/                   # Kubernetes manifests
│   │       ├── 1.namespace.yaml   # Namespace definition
│   │       ├── 2.rbac.yaml        # Service account and RBAC rules
│   │       ├── 3.configmap.yaml   # Application configuration
│   │       ├── 4.deployment.yaml  # Deployment specification
│   │       └── 5.service.yaml     # LoadBalancer service
│   │
│   └── voting-app-k8s/            # Blazor voting application with Orleans
│       ├── Program.cs             # Orleans + Blazor Server configuration
│       ├── Dockerfile             # Multi-stage Docker build
│       ├── Grains/                # Voting grains (Poll, Vote, UserAgent)
│       ├── Pages/                 # Blazor Razor pages
│       └── k8s/                   # Kubernetes manifests
│           ├── 2.redis.yaml       # Redis for clustering & persistence
│           ├── 3.voting-app-reqs.yaml  # Service and RBAC
│           ├── 4.build-push-image.ps1  # Build and deploy script
│           └── 5.voting-app-deployment.yaml  # Deployment specification
```

## 🚀 Sample Applications

### 1. URL Shortener (`orleans-url-shortener`)

A minimal Orleans application that demonstrates URL shortening with persistent state.

**Features:**

- Simple REST API for URL shortening
- Orleans grain with persistent state in memory (for development)
- .NET 8.0

**Key Orleans Configuration:**

```csharp
builder.Host.UseOrleans((ctx, orleansBuilder) =>
{
    orleansBuilder
        .UseLocalhostClustering()      // Development: local clustering
        .AddMemoryGrainStorage("urls"); // Development: in-memory storage
});
```

**Grain Example:**

```csharp
public sealed class UrlShortenerGrain : Grain, IUrlShortenerGrain
{
    private readonly IPersistentState<UrlDetails> _state;

    public async Task SetUrl(string fullUrl)
    {
        _state.State = new() { FullUrl = fullUrl };
        await _state.WriteStateAsync();
    }
}
```

### 2. Voting App (`voting-app-k8s`)

A more complex Orleans application with Blazor Server, real-time updates, and Redis persistence.

**Features:**

- Blazor Server UI with real-time poll updates
- Orleans Dashboard for monitoring
- Redis-based clustering and persistence for production
- Kubernetes hosting support
- .NET 8.0 with Orleans 10.0 (RC)

**Key Orleans Configuration:**

```csharp
builder.Host.UseOrleans((ctx, orleansBuilder) =>
{
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        orleansBuilder
            .UseLocalhostClustering()
            .AddMemoryGrainStorage("votes");
    }
    else
    {
        // Kubernetes hosting with Redis
        orleansBuilder.UseKubernetesHosting();
        var redisAddress = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
        orleansBuilder.UseRedisClustering(options => 
            options.ConfigurationOptions = ConfigurationOptions.Parse(redisAddress));
        orleansBuilder.AddRedisGrainStorage("votes", options => 
            options.ConfigurationOptions = ConfigurationOptions.Parse(redisAddress));
    }
    orleansBuilder.AddDashboard();
});
```

## 🐳 Docker

Both applications use multi-stage Docker builds optimized for .NET 8.0:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080 8081 11111

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Build and publish steps...

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

**Build Example:**

```powershell
# URL Shortener
cd src/orleans-url-shortener
docker build -f web/Dockerfile -t orleans-urlshortener .

# Voting App
cd src/voting-app-k8s
docker build -t votingapp .
```

## ☸️ Kubernetes Deployment

### Prerequisites

1. An Azure subscription
2. Azure CLI installed
3. kubectl configured
4. An AKS cluster
5. An Azure Container Registry (ACR)

### URL Shortener Deployment

```powershell
# Apply Kubernetes manifests in order
kubectl apply -f src/orleans-url-shortener/k8s/1.namespace.yaml
kubectl apply -f src/orleans-url-shortener/k8s/2.rbac.yaml
kubectl apply -f src/orleans-url-shortener/k8s/3.configmap.yaml
kubectl apply -f src/orleans-url-shortener/k8s/4.deployment.yaml
kubectl apply -f src/orleans-url-shortener/k8s/5.service.yaml
```

### Voting App Deployment

```powershell
# Deploy Redis first
kubectl apply -f src/voting-app-k8s/k8s/2.redis.yaml

# Deploy RBAC and Service
kubectl apply -f src/voting-app-k8s/k8s/3.voting-app-reqs.yaml

# Build and push image, then deploy
cd src/voting-app-k8s/k8s
.\4.build-push-image.ps1
```

### Key Kubernetes Components

**RBAC Configuration** - Orleans requires pod discovery for clustering:

```yaml
rules:
- apiGroups: [""]
  resources: ["pods"]
  verbs: ["get", "watch", "list"]
```

**Pod Environment Variables** - Required for Orleans Kubernetes hosting:

```yaml
env:
- name: POD_NAMESPACE
  valueFrom:
    fieldRef:
      fieldPath: metadata.namespace
- name: POD_NAME
  valueFrom:
    fieldRef:
      fieldPath: metadata.name
- name: POD_IP
  valueFrom:
    fieldRef:
      fieldPath: status.podIP
```

**Orleans Labels** - Used for cluster membership:

```yaml
labels:
  orleans/serviceId: myapp
  orleans/clusterId: myapp
```

## 🔧 Local Development

### Run URL Shortener Locally

```powershell
cd src/orleans-url-shortener/web
dotnet run
```

Access at: `http://localhost:5000`

- `GET /shorten?url=https://example.com` - Create shortened URL
- `GET /go/{id}` - Redirect to original URL

### Run Voting App Locally

```powershell
cd src/voting-app-k8s
dotnet run -c Release -- --environment Development --urls http://localhost:5000
```

Access at:

- `http://localhost:5000` - Voting UI
- `http://localhost:5000/dashboard` - Orleans Dashboard

## 📦 NuGet Packages

### URL Shortener

| Package | Version |
| ------- | ------- |
| Microsoft.Orleans.Server | 8.* |
| Orleans.Clustering.Kubernetes | 8.2.1 |

### Voting App

| Package | Version |
| ------- | ------- |
| Microsoft.Orleans.Server | 10.0.0-rc.2 |
| Microsoft.Orleans.Hosting.Kubernetes | 10.0.0-rc.2 |
| Microsoft.Orleans.Clustering.Redis | 10.0.0-rc.2 |
| Microsoft.Orleans.Persistence.Redis | 10.0.0-rc.2 |
| Microsoft.Orleans.Dashboard | 10.0.0-rc.2 |

## 📚 Resources

- [Orleans Documentation](https://learn.microsoft.com/dotnet/orleans/)
- [Orleans on Kubernetes](https://learn.microsoft.com/dotnet/orleans/deployment/kubernetes)
- [AKS Documentation](https://learn.microsoft.com/azure/aks/)
- [Orleans GitHub Repository](https://github.com/dotnet/orleans)

## 📄 License

See [LICENSE](LICENSE) file for details.
