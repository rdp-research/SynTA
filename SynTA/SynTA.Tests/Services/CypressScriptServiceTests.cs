using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Models.Domain;
using SynTA.Services.Database;
using SynTA.Tests.Helpers;

namespace SynTA.Tests.Services
{
    public class CypressScriptServiceTests : IDisposable
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly CypressScriptService _service;
        private readonly Mock<ILogger<CypressScriptService>> _loggerMock;
        private readonly Project _testProject;
        private readonly UserStory _testUserStory;

        public CypressScriptServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<CypressScriptService>>();
            _service = new CypressScriptService(_context, _loggerMock.Object);

            // Create test project and user story
            _testProject = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            _context.Projects.Add(_testProject);
            _context.SaveChanges();

            _testUserStory = new UserStory
            {
                Title = "Test Story",
                UserStoryText = "As a user, test script context",
                Description = "Test Description",
                ProjectId = _testProject.Id
            };
            _context.UserStories.Add(_testUserStory);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateScriptAsync_ValidScript_ReturnsCreatedScript()
        {
            // Arrange
            var script = new CypressScript
            {
                FileName = "login_test.cy.ts",
                Content = "describe('Login', () => { it('should login', () => {}); });",
                TargetUrl = "https://example.com/login",
                UserStoryId = _testUserStory.Id
            };

            // Act
            var result = await _service.CreateScriptAsync(script);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("login_test.cy.ts", result.FileName);
            Assert.Equal(_testUserStory.Id, result.UserStoryId);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task GetScriptByIdAsync_ExistingScript_ReturnsScript()
        {
            // Arrange
            var script = new CypressScript
            {
                FileName = "test.cy.ts",
                Content = "test content",
                UserStoryId = _testUserStory.Id
            };
            await _service.CreateScriptAsync(script);

            // Act
            var result = await _service.GetScriptByIdAsync(script.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(script.Id, result.Id);
            Assert.Equal("test.cy.ts", result.FileName);
        }

        [Fact]
        public async Task GetScriptByIdAsync_NonExistingScript_ReturnsNull()
        {
            // Act
            var result = await _service.GetScriptByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetScriptByIdAsync_IncludesNavigationProperties()
        {
            // Arrange
            var script = new CypressScript
            {
                FileName = "test.cy.ts",
                Content = "test content",
                UserStoryId = _testUserStory.Id
            };
            await _service.CreateScriptAsync(script);

            // Act
            var result = await _service.GetScriptByIdAsync(script.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UserStory);
            Assert.NotNull(result.UserStory.Project);
            Assert.Equal(_testProject.Name, result.UserStory.Project.Name);
        }

        [Fact]
        public async Task GetScriptsByUserStoryIdAsync_MultipleScripts_ReturnsUserStoryScripts()
        {
            // Arrange
            var script1 = new CypressScript { FileName = "test1.cy.ts", Content = "content1", UserStoryId = _testUserStory.Id };
            var script2 = new CypressScript { FileName = "test2.cy.ts", Content = "content2", UserStoryId = _testUserStory.Id };

            // Create another user story with a script
            var otherStory = new UserStory { Title = "Other Story", Description = "Other Desc", ProjectId = _testProject.Id };
            _context.UserStories.Add(otherStory);
            await _context.SaveChangesAsync();

            var script3 = new CypressScript { FileName = "test3.cy.ts", Content = "content3", UserStoryId = otherStory.Id };

            await _service.CreateScriptAsync(script1);
            await _service.CreateScriptAsync(script2);
            await _service.CreateScriptAsync(script3);

            // Act
            var results = await _service.GetScriptsByUserStoryIdAsync(_testUserStory.Id);

            // Assert
            var scriptList = results.ToList();
            Assert.Equal(2, scriptList.Count);
            Assert.All(scriptList, s => Assert.Equal(_testUserStory.Id, s.UserStoryId));
        }

        [Fact]
        public async Task GetScriptsByUserStoryIdAsync_NoScripts_ReturnsEmptyList()
        {
            // Act
            var results = await _service.GetScriptsByUserStoryIdAsync(_testUserStory.Id);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task UpdateScriptAsync_ValidScript_UpdatesAndReturnsScript()
        {
            // Arrange
            var script = new CypressScript
            {
                FileName = "original.cy.ts",
                Content = "original content",
                UserStoryId = _testUserStory.Id
            };
            await _service.CreateScriptAsync(script);

            // Act
            script.FileName = "updated.cy.ts";
            script.Content = "updated content";
            var result = await _service.UpdateScriptAsync(script);

            // Assert
            Assert.Equal("updated.cy.ts", result.FileName);
            Assert.Equal("updated content", result.Content);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task DeleteScriptAsync_ExistingScript_ReturnsTrueAndDeletes()
        {
            // Arrange
            var script = new CypressScript
            {
                FileName = "test.cy.ts",
                Content = "content",
                UserStoryId = _testUserStory.Id
            };
            await _service.CreateScriptAsync(script);
            var scriptId = script.Id;

            // Act
            var result = await _service.DeleteScriptAsync(scriptId);

            // Assert
            Assert.True(result);
            var deletedScript = await _service.GetScriptByIdAsync(scriptId);
            Assert.Null(deletedScript);
        }

        [Fact]
        public async Task DeleteScriptAsync_NonExistingScript_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteScriptAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetScriptsByUserStoryIdAsync_ReturnsOrderedByCreatedAtDescending()
        {
            // Arrange
            var script1 = new CypressScript { FileName = "first.cy.ts", Content = "content1", UserStoryId = _testUserStory.Id, CreatedAt = DateTime.UtcNow.AddMinutes(-1) };
            var script2 = new CypressScript { FileName = "second.cy.ts", Content = "content2", UserStoryId = _testUserStory.Id, CreatedAt = DateTime.UtcNow };

            // Add directly to context so we control timestamps (service would overwrite CreatedAt on Create)
            _context.CypressScripts.AddRange(script1, script2);
            await _context.SaveChangesAsync();

            // Act
            var results = await _service.GetScriptsByUserStoryIdAsync(_testUserStory.Id);

            // Assert
            var scriptList = results.ToList();
            Assert.Equal(2, scriptList.Count);
            Assert.Equal("second.cy.ts", scriptList[0].FileName); // Most recent first
            Assert.Equal("first.cy.ts", scriptList[1].FileName);
        }
    }
}
