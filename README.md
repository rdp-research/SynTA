# SynTA Workspace

This repository contains the SynTA solution and supporting projects for AI-assisted test generation.

SynTA helps users move from user stories to generated Gherkin scenarios and Cypress tests, with optional real-page context extraction from a target URL.

## Repository Layout

- `SynTA/` - solution root (contains `SynTA.slnx`)
- `SynTA/SynTA/` - main ASP.NET Core web application
- `SynTA/SynTA.AppHost/` - .NET Aspire AppHost
- `SynTA/SynTA.ServiceDefaults/` - shared Aspire defaults (health, telemetry, resilience)
- `SynTA/SynTA.Tests/` - xUnit test project

## Quick Start

From this repository root:

```powershell
cd SynTA
dotnet restore SynTA.slnx
dotnet build SynTA.slnx
dotnet test SynTA.slnx
```

Run the web app directly:

```powershell
dotnet run --project SynTA/SynTA.csproj
```

Default local URL from launch settings:

- `https://localhost:7008`

Run with Aspire AppHost instead:

```powershell
dotnet run --project SynTA.AppHost/SynTA.AppHost.csproj
```

## Full Documentation

For full installation, configuration, workflow, architecture, admin setup, Cypress execution, troubleshooting, and deployment notes, see:

- `SynTA/README.md`

## Verified Baseline (Current Workspace)

- .NET SDK: `10.0.104`
- Test suite: `98 passed, 0 failed`