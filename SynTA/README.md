# SynTA

SynTA is an ASP.NET Core application that turns user stories into:

1. AI-generated Gherkin scenarios
2. AI-generated Cypress scripts
3. Optional executable Cypress test runs from inside SynTA

The system supports OpenAI, Gemini, and OpenRouter providers, and can enrich generation with real page context extracted through Playwright.

## Table of Contents

- Overview
- Architecture and Projects
- Requirements
- Installation and First Run
- Running the Application
- Configuration
- End-to-End Workflow
- Admin Area
- Testing and Quality
- Troubleshooting
- Deployment Notes
- License

## Overview

SynTA's primary flow is:

1. User creates a Project
2. User adds a User Story (title, user story text, optional details)
3. SynTA generates Gherkin scenarios
4. User optionally edits generated Gherkin
5. SynTA generates Cypress from Gherkin, optionally using real HTML/screenshot context from a target URL
6. User reviews, downloads, or executes Cypress test runs

## Architecture and Projects

This solution contains:

- `SynTA.slnx` - solution entry
- `SynTA/` - main ASP.NET Core MVC app (.NET 10)
- `SynTA.AppHost/` - .NET Aspire AppHost for orchestration
- `SynTA.ServiceDefaults/` - shared OpenTelemetry/health/service defaults
- `SynTA.Tests/` - xUnit test project

Core technical pieces:

- ASP.NET Core MVC with Areas (`User`, `Admin`)
- ASP.NET Core Identity with roles (`Admin`, `User`)
- EF Core + SQL Server (`ApplicationDbContext`)
- AI provider abstraction (`IAIGenerationService` + `AIServiceFactory`)
- Playwright scraping (`WebScraperService`) and HTML reduction (`HtmlContentProcessor`)
- In-app Cypress execution (`CypressRunnerService`)
- Serilog logging + OpenTelemetry hooks

## Requirements

Mandatory:

- .NET SDK 10.x
- SQL Server-compatible instance (LocalDB on Windows or SQL Server container/server)
- At least one AI API key configured:
  - OpenAI
  - Gemini
  - OpenRouter

Needed for optional or advanced flows:

- Node.js + npm (required for in-app Cypress test execution)
- Playwright browser binaries (required for HTML/screenshot context extraction)

## Installation and First Run

Run these commands from the solution root (`SynTA/` folder that contains `SynTA.slnx`).

1. Restore and build

```powershell
dotnet restore SynTA.slnx
dotnet build SynTA.slnx
```

2. Configure secrets (recommended)

The web project has a user-secrets id configured. Set only what you need.

```powershell
dotnet user-secrets --project SynTA/SynTA.csproj set "OpenAI:ApiKey" "<your-openai-key>"
dotnet user-secrets --project SynTA/SynTA.csproj set "Gemini:ApiKey" "<your-gemini-key>"
dotnet user-secrets --project SynTA/SynTA.csproj set "OpenRouter:ApiKey" "<your-openrouter-key>"
```

3. Select default provider (optional)

`SynTA/appsettings.json` uses:

```json
"AI": {
  "Provider": "OpenAI"
}
```

You can keep this and switch provider per-user in Settings UI later.

4. Ensure database is ready

The app applies pending migrations automatically in Development at startup. You can also apply explicitly:

```powershell
dotnet tool update --global dotnet-ef
dotnet ef database update --project SynTA/SynTA.csproj
```

5. Install Playwright browsers (required for HTML context extraction)

```powershell
dotnet build SynTA/SynTA.csproj
powershell SynTA/bin/Debug/net10.0/playwright.ps1 install
```

Fallback:

```powershell
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

## Running the Application

### Option A: Run web app directly

```powershell
dotnet run --project SynTA/SynTA.csproj
```

Default local URLs from launch settings:

- `https://localhost:7008`
- `http://localhost:5248`

### Option B: Run with Aspire AppHost

```powershell
dotnet run --project SynTA.AppHost/SynTA.AppHost.csproj
```

Use this when you want Aspire orchestration and dashboard behavior.

### Health endpoints

In Development, service defaults map:

- `/health`
- `/alive`

## Configuration

### Main settings file

`SynTA/appsettings.json` includes:

- `ConnectionStrings:DefaultConnection`
- `AI:Provider`
- `OpenAI:*`
- `Gemini:*`
- `OpenRouter:*`
- `Cypress:*`
- `Serilog:*`

### AI provider model-tier mapping

Provider selection and tier are implemented in code.

- OpenAI:
  - `UltraFast` -> `gpt-5-nano`
  - `Fast` -> `gpt-5-mini`
  - `Smart` -> `gpt-5.2`
- Gemini:
  - `UltraFast` -> `gemini-2.5-flash-lite`
  - `Fast` -> `gemini-2.5-flash`
  - `Smart` -> `gemini-3-pro-preview`
- OpenRouter defaults:
  - `UltraFast` -> `google/gemini-2.5-flash-lite`
  - `Fast` -> `openai/gpt-5-mini`
  - `Smart` -> `anthropic/claude-3.7-sonnet`
  - or custom model from user settings

### User-level runtime settings

Each user has persisted settings (created automatically on first access), including:

- Preferred AI provider and model tier
- Preferred output language
- Max scenarios per generation
- Cypress language (`TypeScript` or `JavaScript`)
- Vision API screenshot usage
- Web extraction toggles:
  - page metadata
  - UI element map
  - accessibility tree
  - simplified HTML

## End-to-End Workflow

### 1) Authentication

- Register: `/Account/Register`
- Login: `/Account/Login`

Users are standard Identity users. No seeded default admin user is created by startup.

### 2) Project management

Under User area:

- `/User/Project/Index`
- Create/Edit/Delete projects

Projects are scoped to owner user id.

### 3) User story management

- `/User/UserStory/Index?projectId=<id>`
- Create/Edit/Delete user stories
- Story carries title, user story text, optional description and acceptance criteria

### 4) Gherkin generation

- `/User/Gherkin/Generate?userStoryId=<id>`
- Uses selected AI provider and user settings
- Saves generated content to `GherkinScenarios`
- User can edit or delete generated scenarios

### 5) Cypress generation

- `/User/Cypress/Configure?gherkinScenarioId=<id>`
- Requires target URL input
- Optional fetch of HTML context via Playwright

Important behavior:

- If HTML context fetch is enabled and fetch fails, generation is intentionally aborted to avoid bad selectors.
- If using workflow service (`GenerateAll`) with a target URL, HTML fetch failures are non-blocking and generation continues without context.

### 6) Review and export

- `/User/Cypress/ReviewAndExport/<scriptId>`
- Download exports `.cy.ts` or `.cy.js` based on generated filename/language

### 7) Run Cypress tests inside SynTA

From review screen, `RunTest` launches asynchronous execution.

Execution details:

- Runner initializes a temporary Cypress workspace
- Creates minimal `cypress.config.ts`, `package.json`, and support files
- Runs `npm install` on first use in working directory
- Executes `npx cypress run --spec ... --config baseUrl=...`
- Polling endpoint: `/User/Cypress/GetRunStatus?runId=<guid>`

## Admin Area

Admin routes require policy `RequireAdminRole`:

- `/Admin/Dashboard/Index`
- `/Admin/Users/Index`
- `/Admin/Users/Details/<userId>`

Startup seeds roles (`Admin`, `User`) but does not seed a default admin account.

### Grant admin role to a user

1. Register a user normally.
2. Run SQL against your database:

```sql
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
WHERE u.Email = 'your-admin-email@example.com'
  AND r.Name = 'Admin';
```

Then sign out/in and access admin routes.

## Testing and Quality

Run tests:

```powershell
dotnet test SynTA.slnx --nologo
```

Current verified result in this workspace:

- `98 passed, 0 failed`

Test project includes coverage for:

- domain models
- DB services (project, user story, cypress script)
- AI service factory behavior
- HTML context and processor logic
- image processing logic
- export/file naming behavior

## Troubleshooting

### Playwright executable missing

Symptom:

- Playwright errors about missing browser executable

Fix:

```powershell
powershell SynTA/bin/Debug/net10.0/playwright.ps1 install
```

### `npm install` / Cypress runner fails

Symptom:

- in-app test run fails at runner initialization

Fix:

- Install Node.js and ensure `npm` / `npx` are in PATH.
- Confirm `Cypress:NpxPath` in `appsettings.json` if custom path is needed.

### AI returns empty/blocked/truncated responses

Typical causes:

- model overload or safety filters
- prompt too large (especially with full extraction context)

Fixes:

- switch to faster model tier
- disable some extraction toggles (accessibility tree, simplified HTML, etc.)
- retry without screenshot context (OpenRouter fallback already attempts text-only retry)

### Cannot access admin area

Symptom:

- 403/Access denied on `/Admin/*`

Fix:

- ensure your user has `Admin` role mapping in `AspNetUserRoles`

### `dotnet ef` shows HostAbortedException logs

When using EF tooling with ASP.NET startup, seeing host start/abort logs can be normal as tooling resolves the context. If migrations list or update succeeds, this is not necessarily a failure.

## Deployment Notes

`SynTA/SynTA/Dockerfile` is Playwright-aware and uses:

- `mcr.microsoft.com/playwright/dotnet` base image
- non-root runtime user
- health check against `/health`

For production:

- set secure connection strings and API keys via environment variables
- run with `ASPNETCORE_ENVIRONMENT=Production`
- ensure HTTPS termination and proper secret management

## License

See `LICENSE.txt`.
