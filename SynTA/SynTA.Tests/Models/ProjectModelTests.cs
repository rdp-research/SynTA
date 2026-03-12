using SynTA.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace SynTA.Tests.Models
{
    public class ProjectModelTests
    {
        [Fact]
        public void Project_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var project = new Project();

            // Assert
            Assert.Equal(0, project.Id);
            Assert.Equal(string.Empty, project.Name);
            Assert.Null(project.Description);
            Assert.Equal(string.Empty, project.UserId);
            Assert.Null(project.UpdatedAt);
            Assert.NotNull(project.UserStories);
            Assert.Empty(project.UserStories);
        }

        [Fact]
        public void Project_CreatedAt_DefaultsToUtcNow()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var project = new Project();

            // Assert
            Assert.True(project.CreatedAt >= beforeCreation);
            Assert.True(project.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void Project_CanSetAllProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow.AddHours(1);

            // Act
            var project = new Project
            {
                Id = 1,
                Name = "Test Project",
                Description = "Test Description",
                UserId = "user123",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.Equal(1, project.Id);
            Assert.Equal("Test Project", project.Name);
            Assert.Equal("Test Description", project.Description);
            Assert.Equal("user123", project.UserId);
            Assert.Equal(createdAt, project.CreatedAt);
            Assert.Equal(updatedAt, project.UpdatedAt);
        }

        [Fact]
        public void Project_Name_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(Project).GetProperty(nameof(Project.Name));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }

        [Fact]
        public void Project_Name_HasMaxLengthOf200()
        {
            // Arrange
            var property = typeof(Project).GetProperty(nameof(Project.Name));

            // Act
            var maxLengthAttr = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
                .Cast<MaxLengthAttribute>().FirstOrDefault();

            // Assert
            Assert.NotNull(maxLengthAttr);
            Assert.Equal(200, maxLengthAttr.Length);
        }

        [Fact]
        public void Project_Description_HasMaxLengthOf1000()
        {
            // Arrange
            var property = typeof(Project).GetProperty(nameof(Project.Description));

            // Act
            var maxLengthAttr = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
                .Cast<MaxLengthAttribute>().FirstOrDefault();

            // Assert
            Assert.NotNull(maxLengthAttr);
            Assert.Equal(1000, maxLengthAttr.Length);
        }

        [Fact]
        public void Project_UserId_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(Project).GetProperty(nameof(Project.UserId));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }
    }
}
