# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Install system dependencies for Playwright + git for self-update
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    git \
    libnss3 \
    libnspr4 \
    libatk1.0-0t64 \
    libatk-bridge2.0-0t64 \
    libcups2t64 \
    libdrm2 \
    libdbus-1-3 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2t64 \
    libpango-1.0-0 \
    libcairo2 \
    libxshmfence1 \
    libx11-6 \
    libx11-xcb1 \
    libxext6 \
    libxrender1 \
    libxss1 \
    libgtk-3-0t64 \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# Copy NuGet configuration
COPY NuGet.config ./NuGet.config

# Explicitly add NuGet sources with increased timeout
RUN dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org || true && \
    dotnet nuget add source https://boltnuget.boltqa.com/nuget -n bolt || true

# Set NuGet HTTP timeout to 10 minutes
ENV NUGET_HTTP_REQUEST_TIMEOUT=600

# Copy solution and project files
# Directory.Packages.props must be present at restore time so Central Package
# Management resolves the centrally-managed versions; without it, projects whose
# <PackageReference> entries have no Version attribute fall back to NuGet's
# lowest-version heuristic (1.0.0) and pull in vulnerable / wrong-TFM packages.
COPY *.sln ./
COPY Directory.Packages.props ./
COPY **/*.csproj ./
RUN for file in $(ls *.csproj); do \
        mkdir -p ${file%.*}/ && mv $file ${file%.*}/; \
    done

# Restore NuGet packages
RUN dotnet restore Bolt.Automation.sln --configfile ./NuGet.config

# Copy the rest of the source code (including .git for self-update)
COPY . .

# Configure git safe directory and bake the current commit hash for fallback
# when git is unavailable at runtime (network issues, stripped images, etc.)
RUN git config --global --add safe.directory /app && \
    git rev-parse HEAD > /app/COMMIT_HASH

# Build tests AND worker agent
RUN dotnet build Bolt.Automation.Tests/Bolt.Automation.Tests.csproj -c Release --no-restore
RUN dotnet publish Bolt.Automation.WorkerAgent/Bolt.Automation.WorkerAgent.csproj \
    -c Release -o /app/agent --no-restore

# Install Playwright Chromium (supports both amd64 and arm64)
RUN pwsh Bolt.Automation.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium

# Set environment variables (build-time + defaults that must match image layout)
# Runtime vars (ORCHESTRATOR_URL, API_KEY, WORKER_CONCURRENCY, etc.) are set via
# docker-compose or Kubernetes — see WorkerOptions.cs for code-level defaults
ENV ASPNETCORE_ENVIRONMENT=QA \
    PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright \
    CI=1 \
    BOLT_SECRETS_PATH=/mnt/bolt-secrets

# AZURE_DEVOPS_PAT is supplied by the K8s Deployment via a Secret env var
# (no longer baked into the image — see fix/worker-git-config-scrub for the
# full rationale). The worker scrubs any inherited http.*.extraheader entries
# from .git/config at startup so a per-command -c override is the only
# Authorization header at fetch time.

# Create results directory
RUN mkdir -p /app/results

# Health (/healthz, /readyz) and metrics (/metrics) endpoint
EXPOSE 8080

# Worker agent as entrypoint
ENTRYPOINT ["dotnet", "/app/agent/Bolt.Automation.WorkerAgent.dll"]
