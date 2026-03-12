# SynTA - Quick Reference Guide

## Common Tasks

### For Developers

#### Running the Application
```bash
# Development
dotnet run --project SynTA

# With hot reload
dotnet watch --project SynTA
```

#### Database Operations
```bash
# Create new migration
dotnet ef migrations add MigrationName --project SynTA

# Update database
dotnet ef database update --project SynTA

# Remove last migration
dotnet ef migrations remove --project SynTA

# Generate SQL script
dotnet ef migrations script --project SynTA --output migration.sql
```

#### Building and Testing
```bash
# Build
dotnet build

# Run tests
dotnet test

# Build for production
dotnet publish -c Release
```

### For Administrators

#### Default Admin Access
- **URL**: `https://localhost:5001/Admin/Dashboard`
- **Email**: `admin@synta.com`
- **Password**: `Admin@123`
- ⚠️ **Change immediately in production!**

#### Admin Routes
- Dashboard: `/Admin/Dashboard/Index`
- User List: `/Admin/Users/Index`
- User Details: `/Admin/Users/Details/{userId}`

#### Admin Actions
- **Lock User**: Users ? Details ? Lock Account
- **Unlock User**: Users ? Details ? Unlock Account
- **Grant Admin**: Users ? Details ? Grant Admin Role
- **Remove Admin**: Users ? Details ? Remove Admin Role
- **Delete User**: Users ? Details ? Delete User (permanent!)

### For QA/Testers

#### Test User Creation
```sql
-- Run in SQL Server Management Studio
-- Note: Replace password hash with actual hashed password
INSERT INTO AspNetUsers (Id, UserName, Email, NormalizedUserName, NormalizedEmail, EmailConfirmed)
VALUES (NEWID(), 'test@example.com', 'test@example.com', 'TEST@EXAMPLE.COM', 'TEST@EXAMPLE.COM', 1);
```

#### Testing Workflow
1. Create test project
2. Add user story
3. Generate Gherkin
4. Approve scenario
5. Generate Cypress
6. Download and validate script

#### Validating Generated Scripts
```bash
# Install Cypress in test folder
npm install cypress --save-dev

# Copy generated script
cp downloaded.cy.ts cypress/e2e/

# Run tests
npx cypress run --spec "cypress/e2e/downloaded.cy.ts"
```

### For DevOps

#### Environment Variables (Production)
```bash
ConnectionStrings__DefaultConnection="Server=...;Database=SynTADb;..."
OpenAI__ApiKey="sk-..."
OpenAI__Model="gpt-4"
ASPNETCORE_ENVIRONMENT="Production"
```

#### Docker Commands
```bash
# Build image
docker build -t synta:latest -f SynTA/Dockerfile .

# Run container
docker run -d -p 80:80 --name synta synta:latest

# View logs
docker logs synta -f

# Stop container
docker stop synta

# Remove container
docker rm synta
```

#### Health Check
```bash
curl https://your-domain.com/health
```

## Important URLs

### User Area
- Home: `/`
- My Projects: `/User/Project/Index`
- User Stories: `/User/UserStory/Index?projectId={id}`
- Gherkin Scenarios: `/User/Gherkin/Index?userStoryId={id}`
- Cypress Scripts: `/User/Cypress/Index?userStoryId={id}`

### Admin Area
- Dashboard: `/Admin/Dashboard/Index`
- Users: `/Admin/Users/Index`
- User Details: `/Admin/Users/Details/{userId}`

### Authentication
- Login: `/Identity/Account/Login`
- Register: `/Identity/Account/Register`
- Logout: `/Identity/Account/Logout`

## Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "OpenAI": {
    "ApiKey": "...",
    "Model": "gpt-4",
    "MaxTokens": 2000
  }
}
```

### User Secrets (Development)
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..."
dotnet user-secrets set "OpenAI:ApiKey" "..."
```

## Database Schema

### Main Tables
- `AspNetUsers` - User accounts
- `AspNetRoles` - User roles (Admin, User)
- `AspNetUserRoles` - User-Role mapping
- `Projects` - User projects
- `UserStories` - User stories per project
- `GherkinScenarios` - Generated Gherkin scenarios
- `CypressScripts` - Generated Cypress test scripts

### Key Relationships
```
ApplicationUser ? Projects (1:Many)
Project ? UserStories (1:Many)
UserStory ? GherkinScenarios (1:Many)
UserStory ? CypressScripts (1:Many)
```

## Service Layer

### Database Services
- `IProjectService` - Project CRUD operations
- `IUserStoryService` - User story management
- `IGherkinScenarioService` - Gherkin scenario management
- `ICypressScriptService` - Cypress script management

### AI Services
- `IAIGenerationService` - AI generation interface (supports multiple providers)
- `IAIServiceFactory` - Factory for creating AI services based on configuration
- `OpenAIService` - OpenAI/GPT implementation
- `GeminiService` - Google Gemini implementation
- `IHtmlContextService` - HTML fetching and parsing

### Export Services
- `IFileExportService` - File generation and export

## Common Issues & Solutions

### Issue: Database Connection Fails
**Solution**: Check connection string and SQL Server is running
```bash
# Test connection
sqlcmd -S localhost -U sa -P YourPassword
```

### Issue: Migrations Fail
**Solution**: Drop database and recreate
```bash
dotnet ef database drop --project SynTA
dotnet ef database update --project SynTA
```

### Issue: Admin Can't Access Admin Area
**Solution**: Check user has Admin role
```sql
-- Grant admin role
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id 
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'user@example.com' AND r.Name = 'Admin';
```

### Issue: OpenAI API Errors
**Solution**: 
1. Verify API key is correct
2. Check API quota/billing
3. Verify model name is correct
4. Check network connectivity

## Performance Tips

### For Better Response Times
1. Enable response caching
2. Add database indexes (already done)
3. Optimize AI prompts
4. Use connection pooling
5. Enable output caching

### Monitoring
```csharp
// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();
```

## Security Best Practices

1. ? Always use HTTPS in production
2. ? Change default admin password
3. ? Keep API keys in secrets/environment variables
4. ? Enable CORS properly
5. ? Use strong passwords
6. ? Regular security updates
7. ? Enable rate limiting
8. ? Validate all inputs
9. ? Use anti-forgery tokens
10. ? Regular backups

## Useful Commands

### Git
```bash
git status
git add .
git commit -m "Your message"
git push origin develop
```

### NuGet
```bash
dotnet add package PackageName
dotnet restore
dotnet list package
```

### Logs
```bash
# View application logs
tail -f /var/log/synta/log-*.txt

# Docker logs
docker logs synta --tail 100 -f
```

## Support Resources

- **Documentation**: `/SynTA/ADMIN_README.md`, `TESTING_GUIDE.md`, `DEPLOYMENT_GUIDE.md`
- **GitHub**: https://github.com/vancoder1/SynTA
- **Issues**: Create issue on GitHub

## Team Contacts

- **Backend/AI**: Ivan Zaporozhets
- **Frontend/UI**: Harshan Gidda
- **Database/QA**: Lukas Dreise

---

**Last Updated**: Phase 6 Implementation  
**Version**: 1.0.0
