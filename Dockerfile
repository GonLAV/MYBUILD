# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /app

# Install system dependencies for Playwright
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
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
    libfontconfig1 \
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

# Copy the rest of the source code
COPY . .

# Build the test project
RUN dotnet build Bolt.Automation.Tests/Bolt.Automation.Tests.csproj -c Release --no-restore

# Install Playwright browsers (chromium, chrome, firefox, webkit)
RUN pwsh Bolt.Automation.Tests/bin/Release/net10.0/playwright.ps1 install --with-deps chrome

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=QA \
    HEADLESS=true \
    ARTIFACTS_PATH=/app/artifacts \
    PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright \
    CI=1

# Create artifacts directory
RUN mkdir -p /app/artifacts

# Set the entrypoint
ENTRYPOINT ["pwsh", "-NoLogo", "-File", "/app/scripts/test-runner.ps1"]
