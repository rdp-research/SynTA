using Microsoft.Extensions.Logging;
using Moq;
using SynTA.Models.Domain;
using SynTA.Services.Database;
using SynTA.Tests.Helpers;

namespace SynTA.Tests.Services
{
    public class ProjectServiceTests : IDisposable
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly ProjectService _service;
        private readonly Mock<ILogger<ProjectService>> _loggerMock;

        public ProjectServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<ProjectService>>();
            _service = new ProjectService(_context, _loggerMock.Object);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task CreateProjectAsync_ValidProject_ReturnsCreatedProject()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                Description = "Test Description",
                UserId = "user123"
            };

            // Act
            var result = await _service.CreateProjectAsync(project);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("Test Project", result.Name);
            Assert.Equal("user123", result.UserId);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task GetProjectByIdAsync_ExistingProject_ReturnsProject()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);

            // Act
            var result = await _service.GetProjectByIdAsync(project.Id, "user123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(project.Id, result.Id);
            Assert.Equal("Test Project", result.Name);
        }

        [Fact]
        public async Task GetProjectByIdAsync_NonExistingProject_ReturnsNull()
        {
            // Act
            var result = await _service.GetProjectByIdAsync(999, "user123");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProjectByIdAsync_WrongUserId_ReturnsNull()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);

            // Act
            var result = await _service.GetProjectByIdAsync(project.Id, "different-user");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllProjectsByUserIdAsync_MultipleProjects_ReturnsUserProjects()
        {
            // Arrange
            var project1 = new Project { Name = "Project 1", UserId = "user123" };
            var project2 = new Project { Name = "Project 2", UserId = "user123" };
            var project3 = new Project { Name = "Project 3", UserId = "other-user" };

            await _service.CreateProjectAsync(project1);
            await _service.CreateProjectAsync(project2);
            await _service.CreateProjectAsync(project3);

            // Act
            var results = await _service.GetAllProjectsByUserIdAsync("user123");

            // Assert
            var projectList = results.ToList();
            Assert.Equal(2, projectList.Count);
            Assert.All(projectList, p => Assert.Equal("user123", p.UserId));
        }

        [Fact]
        public async Task GetAllProjectsByUserIdAsync_NoProjects_ReturnsEmptyList()
        {
            // Act
            var results = await _service.GetAllProjectsByUserIdAsync("nonexistent-user");

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task UpdateProjectAsync_ValidProject_UpdatesAndReturnsProject()
        {
            // Arrange
            var project = new Project
            {
                Name = "Original Name",
                Description = "Original Description",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);

            // Act
            project.Name = "Updated Name";
            project.Description = "Updated Description";
            var result = await _service.UpdateProjectAsync(project);

            // Assert
            Assert.Equal("Updated Name", result.Name);
            Assert.Equal("Updated Description", result.Description);
            Assert.NotNull(result.UpdatedAt);
        }

        [Fact]
        public async Task DeleteProjectAsync_ExistingProject_ReturnsTrueAndDeletes()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);
            var projectId = project.Id;

            // Act
            var result = await _service.DeleteProjectAsync(projectId, "user123");

            // Assert
            Assert.True(result);
            var deletedProject = await _service.GetProjectByIdAsync(projectId, "user123");
            Assert.Null(deletedProject);
        }

        [Fact]
        public async Task DeleteProjectAsync_NonExistingProject_ReturnsFalse()
        {
            // Act
            var result = await _service.DeleteProjectAsync(999, "user123");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteProjectAsync_WrongUserId_ReturnsFalse()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);

            // Act
            var result = await _service.DeleteProjectAsync(project.Id, "wrong-user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserOwnsProjectAsync_UserOwnsProject_ReturnsTrue()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);

            // Act
            var result = await _service.UserOwnsProjectAsync(project.Id, "user123");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UserOwnsProjectAsync_UserDoesNotOwnProject_ReturnsFalse()
        {
            // Arrange
            var project = new Project
            {
                Name = "Test Project",
                UserId = "user123"
            };
            await _service.CreateProjectAsync(project);

            // Act
            var result = await _service.UserOwnsProjectAsync(project.Id, "other-user");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UserOwnsProjectAsync_NonExistingProject_ReturnsFalse()
        {
            // Act
            var result = await _service.UserOwnsProjectAsync(999, "user123");

            // Assert
            Assert.False(result);
        }
    }
}
