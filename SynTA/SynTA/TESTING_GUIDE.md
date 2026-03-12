# SynTA - Testing and QA Guide

## Overview
This document provides comprehensive testing guidelines for the SynTA application, covering unit tests, integration tests, and end-to-end validation of the generated Cypress scripts.

## Testing Strategy

### 1. Unit Testing
Focus on testing individual components and services in isolation.

#### Services to Test

**Database Services**:
- `ProjectService`
- `UserStoryService`
- `GherkinScenarioService`
- `CypressScriptService`

**AI Services**:
- `OpenAIService`
- `HtmlContextService`

**Export Services**:
- `FileExportService`

#### Sample Unit Test Structure
```csharp
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using SynTA.Services.Database;
using SynTA.Data;
using SynTA.Models.Domain;

namespace SynTA.Tests.Services
{
    public class ProjectServiceTests
    {
        [Fact]
        public async Task GetAllProjectsByUserIdAsync_ReturnsUserProjects()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            
            using var context = new ApplicationDbContext(options);
            var service = new ProjectService(context, Mock.Of<ILogger<ProjectService>>());
            
            var userId = "test-user-id";
            var project = new Project 
            { 
                UserId = userId, 
                Name = "Test Project" 
            };
            context.Projects.Add(project);
            await context.SaveChangesAsync();
            
            // Act
            var result = await service.GetAllProjectsByUserIdAsync(userId);
            
            // Assert
            Assert.Single(result);
            Assert.Equal("Test Project", result.First().Name);
        }
    }
}
```

### 2. Integration Testing
Test the interaction between different components and services.

#### Areas to Test

**Controller Integration**:
- User area controllers (Project, UserStory, Gherkin, Cypress)
- Admin area controllers (Dashboard, Users)

**Database Integration**:
- Entity relationships
- Cascade deletes
- Data integrity

**AI Service Integration**:
- Gherkin generation from user stories
- Cypress script generation from Gherkin scenarios
- HTML context fetching

#### Sample Integration Test
```csharp
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;

namespace SynTA.Tests.Integration
{
    public class UserStoryControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        
        public UserStoryControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }
        
        [Fact]
        public async Task Index_ReturnsSuccessStatusCode()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/User/UserStory/Index?projectId=1");
            
            // Assert
            response.EnsureSuccessStatusCode();
        }
    }
}
```

### 3. End-to-End Testing

#### Testing the Complete Workflow

**Test Case 1: Project Creation to Cypress Export**
1. Create a new project
2. Add a user story with acceptance criteria
3. Generate Gherkin scenarios
4. Review and approve Gherkin scenarios
5. Configure Cypress generation with target URL
6. Generate Cypress script
7. Review generated script
8. Export/Download script
9. Validate script syntax

**Expected Outcomes**:
- All steps complete without errors
- Generated Gherkin scenarios match user story requirements
- Generated Cypress script is syntactically valid TypeScript
- Script contains proper selectors and assertions

#### Test Case 2: Admin Functionality
1. Log in as admin user
2. Access admin dashboard
3. View user management
4. View user details
5. Test user lockout/unlock
6. Test role assignment

**Expected Outcomes**:
- Dashboard displays correct statistics
- All user management functions work correctly
- Role changes take effect immediately

### 4. Validating Generated Cypress Scripts

#### Manual Validation Process

1. **Syntax Validation**:
   ```bash
   # Run TypeScript compiler on generated scripts
   npx tsc --noEmit script-name.cy.ts
   ```

2. **Create Test Sample Application**:
   - Set up a simple web application for testing
   - Ensure it has the elements referenced in user stories

3. **Run Generated Scripts**:
   ```bash
   # Install Cypress if not already installed
   npm install cypress --save-dev
   
   # Copy generated script to cypress/e2e folder
   cp generated-script.cy.ts cypress/e2e/
   
   # Run Cypress
   npx cypress run --spec "cypress/e2e/generated-script.cy.ts"
   ```

4. **Validation Checklist**:
   - [ ] Script runs without syntax errors
   - [ ] Selectors correctly identify elements
   - [ ] Test assertions are appropriate
   - [ ] Test completes successfully
   - [ ] Edge cases are handled
   - [ ] Error messages are clear

#### Sample Web Applications for Testing

**Simple Login Form**:
```html
<!DOCTYPE html>
<html>
<head>
    <title>Test Login</title>
</head>
<body>
    <form id="loginForm">
        <input type="email" id="email" name="email" placeholder="Email" />
        <input type="password" id="password" name="password" placeholder="Password" />
        <button type="submit" id="loginButton">Login</button>
    </form>
    <div id="message" style="display:none;">Login successful!</div>
    <script>
        document.getElementById('loginForm').addEventListener('submit', (e) => {
            e.preventDefault();
            document.getElementById('message').style.display = 'block';
        });
    </script>
</body>
</html>
```

**E-commerce Product List**:
```html
<!DOCTYPE html>
<html>
<head>
    <title>Test Shop</title>
</head>
<body>
    <div id="products">
        <div class="product" data-id="1">
            <h3>Product 1</h3>
            <span class="price">$10.00</span>
            <button class="add-to-cart">Add to Cart</button>
        </div>
        <div class="product" data-id="2">
            <h3>Product 2</h3>
            <span class="price">$20.00</span>
            <button class="add-to-cart">Add to Cart</button>
        </div>
    </div>
    <div id="cart">
        <span id="cart-count">0</span> items
    </div>
</body>
</html>
```

### 5. Performance Testing

#### Metrics to Monitor
- Time to generate Gherkin scenarios
- Time to generate Cypress scripts
- Database query performance
- API response times
- Memory usage during AI operations

#### Tools
- Application Insights (if using Azure)
- Built-in .NET performance counters
- SQL Server profiler
- Browser developer tools

### 6. Security Testing

#### Areas to Validate
- Authentication works correctly
- Authorization prevents unauthorized access
- Admin area is properly secured
- User can only access their own data
- SQL injection prevention (Entity Framework)
- XSS prevention (Razor encoding)
- CSRF token validation

### 7. Test Data Management

#### Sample Test Data

**Test User Stories**:
1. Login functionality
2. User registration
3. Product search
4. Shopping cart operations
5. Checkout process
6. Password reset
7. Profile management
8. File upload
9. Form validation
10. Navigation testing

#### Creating Test Data
```sql
-- Sample test users (run after application startup)
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp)
VALUES 
('test-user-1', 'test1@synta.com', 'TEST1@SYNTA.COM', 'test1@synta.com', 'TEST1@SYNTA.COM', 1, 'HASH', 'STAMP', 'TOKEN'),
('test-user-2', 'test2@synta.com', 'TEST2@SYNTA.COM', 'test2@synta.com', 'TEST2@SYNTA.COM', 1, 'HASH', 'STAMP', 'TOKEN');

-- Sample projects
INSERT INTO Projects (Name, Description, UserId, CreatedAt)
VALUES 
('Test Project 1', 'E-commerce Testing', 'test-user-1', GETUTCDATE()),
('Test Project 2', 'Authentication Testing', 'test-user-1', GETUTCDATE());
```

### 8. Continuous Integration Testing

#### Recommended CI/CD Pipeline

```yaml
# Example GitHub Actions workflow
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Run Unit Tests
      run: dotnet test --no-build --verbosity normal
    
    - name: Run Integration Tests
      run: dotnet test --no-build --filter Category=Integration
```

### 9. Bug Tracking and Reporting

#### Bug Report Template
```markdown
**Title**: [Brief description]

**Severity**: Critical / High / Medium / Low

**Steps to Reproduce**:
1. Step 1
2. Step 2
3. Step 3

**Expected Result**: 
[What should happen]

**Actual Result**: 
[What actually happens]

**Environment**:
- OS: Windows/Linux/Mac
- Browser: Chrome/Firefox/Edge
- .NET Version: 10.0
- Database: SQL Server

**Screenshots/Logs**: 
[Attach relevant screenshots or log files]

**Additional Notes**:
[Any other relevant information]
```

### 10. Test Coverage Goals

#### Recommended Coverage Targets
- **Unit Tests**: 80%+ code coverage
- **Integration Tests**: All critical paths
- **E2E Tests**: Complete user workflows
- **Admin Functions**: 100% coverage

### 11. Quality Metrics

#### Track the Following
- Number of bugs found per release
- Test pass/fail rate
- Code coverage percentage
- Average time to fix bugs
- User-reported issues
- Generated script success rate

## Testing Schedule

### Daily
- Run unit tests on code changes
- Validate build success

### Weekly
- Run full integration test suite
- Test generated Cypress scripts
- Review bug reports

### Before Each Release
- Complete end-to-end testing
- Performance testing
- Security audit
- Admin functionality verification
- Documentation review

## Tools and Resources

### Recommended Testing Tools
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework
- **Cypress**: E2E testing (for validating generated scripts)
- **Postman**: API testing
- **SQL Server Management Studio**: Database testing
- **Visual Studio Test Explorer**: Test runner

### Useful Resources
- [xUnit Documentation](https://xunit.net/)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [Cypress Documentation](https://docs.cypress.io/)
- [Entity Framework Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)

## Conclusion

Quality assurance is critical for the SynTA application, especially given its role in generating test code for other applications. Follow this guide to ensure comprehensive testing coverage and maintain high quality standards throughout the development lifecycle.

**Remember**: The quality of the generated tests depends on the quality of our own testing!
