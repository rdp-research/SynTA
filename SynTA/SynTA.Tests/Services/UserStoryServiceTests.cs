using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Models.Domain;
using SynTA.Services.Database;
using SynTA.Tests.Helpers;

namespace SynTA.Tests.Services
{
    public class UserStoryServiceTests : IDisposable
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly UserStoryService _service;
        private readonly Mock<ILogger<UserStoryService>> _loggerMock;
        private readonly Project _testProject;

        public UserStoryServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<UserStoryService>>();
            _service = new UserStoryService(_context, _loggerMock.Object);

            // Create a test project for user stories
            _testProject = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            _context.Projects.Add(_testProject);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateUserStoryAsync_ValidUserStory_ReturnsCreatedUserStory()
        {
            // Arrange
            var userStory = new UserStory
            {
                Title = "Test Story",
                UserStoryText = "As a user, I want to test something",
                Description = "As a user, I want to test something",
                AcceptanceCriteria = "Given, When, Then",
                ProjectId = _testProject.Id
            };

            // Act
            var result = await _service.CreateUserStoryAsync(userStory);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Test Story", result.Title);
            Assert.Equal(_testProject.Id, result.ProjectId);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task GetUserStoryByIdAsync_ExistingUserStory_ReturnsUserStory()
        {
            // Arrange
            var userStory = new UserStory
            {
                Title = "Test Story",
                UserStoryText = "As a user, I want to test",
                Description = "Test Description",
                ProjectId = _testProject.Id
            };
            await _service.CreateUserStoryAsync(userStory);

            // Act
            var result = await _service.GetUserStoryByIdAsync(userStory.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userStory.Id, result.Id);
            Assert.Equal("Test Story", result.Title);
        }

        [Fact]
        public async Task GetUserStoryByIdAsync_NonExistingUserStory_ReturnsNull()
        {
            // Act
            var result = await _service.GetUserStoryByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllUserStoriesByProjectIdAsync_MultipleStories_ReturnsProjectStories()
        {
            // Arrange
            var story1 = new UserStory { Title = "Story 1", UserStoryText = "Story 1 text", Description = "Desc 1", ProjectId = _testProject.Id };
            var story2 = new UserStory { Title = "Story 2", UserStoryText = "Story 2 text", Description = "Desc 2", ProjectId = _testProject.Id };

            // Create another project with a story
            var otherProject = new Project { Name = "Other Project", UserId = "user456" };
            _context.Projects.Add(otherProject);
            await _context.SaveChangesAsync();

            var story3 = new UserStory { Title = "Story 3", UserStoryText = "Story 3 text", Description = "Desc 3", ProjectId = otherProject.Id };

            await _service.CreateUserStoryAsync(story1);
            await _service.CreateUserStoryAsync(story2);
            await _service.CreateUserStoryAsync(story3);

            // Act
            var results = await _service.GetAllUserStoriesByProjectIdAsync(_testProject.Id);

            // Assert
            var storyList = results.ToList();
            Assert.Equal(2, storyList.Count);
            Assert.All(storyList, s => Assert.Equal(_testProject.Id, s.ProjectId));
        }

        [Fact]
        public async Task GetAllUserStoriesByProjectIdAsync_NoStories_ReturnsEmptyList()
        {
            // Act
            var results = await _service.GetAllUserStoriesByProjectIdAsync(_testProject.Id);

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task UpdateUserStoryAsync_ValidUserStory_UpdatesAndReturnsUserStory()
        {
            // Arrange
            var userStory = new UserStory
            {
                Title = "Original Title",
                UserStoryText = "Original user story text",
                Description = "Original Description",
                ProjectId = _testProject.Id
            };
            await _service.CreateUserStoryAsync(userStory);

            // Act
            userStory.Title = "Updated Title";
            userStory.UserStoryText = "Updated user story text";
            userStory.Description = "Updated Description";
            var result = await _service.UpdateUserStoryAsync(userStory);

            // Assert
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Description", result.Description);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task DeleteUserStoryAsync_ExistingUserStory_ReturnsTrueAndDeletes()
        {
            // Arrange
            var userStory = new UserStory
            {
                Title = "Test Story",
                UserStoryText = "As a user, I want to delete",
                Description = "Test Description",
                ProjectId = _testProject.Id
            };
            await _service.CreateUserStoryAsync(userStory);
            var storyId = userStory.Id;

            // Act
            var result = await _service.DeleteUserStoryAsync(storyId);

            // Assert
            Assert.True(result);
            var deletedStory = await _service.GetUserStoryByIdAsync(storyId);
            Assert.Null(deletedStory);
        }

        [Fact]
        public async Task DeleteUserStoryAsync_NonExistingUserStory_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteUserStoryAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserStoryExistsAsync_ExistingStory_ReturnsTrue()
        {
            // Arrange
            var userStory = new UserStory
            {
                Title = "Test Story",
                UserStoryText = "As a user, I exist",
                Description = "Test Description",
                ProjectId = _testProject.Id
            };
            await _service.CreateUserStoryAsync(userStory);

            // Act
            var result = await _service.UserStoryExistsAsync(userStory.Id);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserStoryExistsAsync_NonExistingStory_ReturnsFalse()
        {
            // Act
            var result = await _service.UserStoryExistsAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetUserStoryByIdAsync_IncludesProject_ReturnsProjectWithStory()
        {
            // Arrange
            var userStory = new UserStory
            {
                Title = "Test Story",
                UserStoryText = "As a user, I want to test",
                Description = "Test Description",
                ProjectId = _testProject.Id
            };
            await _service.CreateUserStoryAsync(userStory);

            // Act
            var result = await _service.GetUserStoryByIdAsync(userStory.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Project);
            Assert.Equal(_testProject.Name, result.Project.Name);
        }
    }
}
