# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# ============================================
# Stage 1: Base image with Playwright support
# ============================================
# Use Playwright-enabled base image that includes Chromium browser and dependencies
FROM mcr.microsoft.com/playwright/dotnet:v1.48-noble AS base
USER root
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install additional runtime dependencies if needed
RUN apt-get update && apt-get install -y \
    libnss3 \
    libatk-bridge2.0-0 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    && rm -rf /var/lib/apt/lists/*

# Create app user for running the application (security best practice)
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app


# ============================================
# Stage 2: Build the service project
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SynTA/SynTA.csproj", "SynTA/"]
RUN dotnet restore "./SynTA/SynTA.csproj"
COPY . .
WORKDIR "/src/SynTA"
RUN dotnet build "./SynTA.csproj" -c $BUILD_CONFIGURATION -o /app/build


# ============================================
# Stage 3: Publish the service project
# ============================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SynTA.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false


# ============================================
# Stage 4: Final production image
# ============================================
FROM base AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Switch to non-root user for security
USER appuser

# Environment variables for Playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1

# Health check (optional - adjust endpoint as needed)
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "SynTA.dll"]