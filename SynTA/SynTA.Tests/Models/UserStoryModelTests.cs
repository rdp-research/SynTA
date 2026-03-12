using SynTA.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace SynTA.Tests.Models
{
    public class UserStoryModelTests
    {
        [Fact]
        public void UserStory_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var userStory = new UserStory();

            // Assert
            Assert.Equal(0, userStory.Id);
            Assert.True(string.IsNullOrEmpty(userStory.Title));
            Assert.True(string.IsNullOrEmpty(userStory.UserStoryText));
            Assert.True(string.IsNullOrEmpty(userStory.Description));
            Assert.Null(userStory.AcceptanceCriteria);
            Assert.Equal(0, userStory.ProjectId);
            Assert.Null(userStory.UpdatedAt);
            Assert.NotNull(userStory.GherkinScenarios);
            Assert.NotNull(userStory.CypressScripts);
            Assert.Empty(userStory.GherkinScenarios);
            Assert.Empty(userStory.CypressScripts);
        }

        [Fact]
        public void UserStory_CreatedAt_DefaultsToUtcNow()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var userStory = new UserStory();

            // Assert
            Assert.True(userStory.CreatedAt >= beforeCreation);
            Assert.True(userStory.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void UserStory_CanSetAllProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow.AddHours(1);

            // Act
            var userStory = new UserStory
            {
                Id = 1,
                Title = "Test Story",
                UserStoryText = "As a user, I want to test",
                Description = "As a user, I want to test",
                AcceptanceCriteria = "Given, When, Then",
                ProjectId = 10,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.Equal(1, userStory.Id);
            Assert.Equal("Test Story", userStory.Title);
            Assert.Equal("As a user, I want to test", userStory.UserStoryText);
            Assert.Equal("As a user, I want to test", userStory.Description);
            Assert.Equal("Given, When, Then", userStory.AcceptanceCriteria);
            Assert.Equal(10, userStory.ProjectId);
            Assert.Equal(createdAt, userStory.CreatedAt);
            Assert.Equal(updatedAt, userStory.UpdatedAt);
        }

        [Fact]
        public void UserStory_Title_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(UserStory).GetProperty(nameof(UserStory.Title));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }

        [Fact]
        public void UserStory_Title_HasMaxLengthOf300()
        {
            // Arrange
            var property = typeof(UserStory).GetProperty(nameof(UserStory.Title));

            // Act
            var maxLengthAttr = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
                .Cast<MaxLengthAttribute>().FirstOrDefault();

            // Assert
            Assert.NotNull(maxLengthAttr);
            Assert.Equal(300, maxLengthAttr.Length);
        }

        [Fact]
        public void UserStory_Description_IsOptional()
        {
            // Arrange
            var property = typeof(UserStory).GetProperty(nameof(UserStory.Description));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.True(requiredAttr == null || requiredAttr.Length == 0);
        }

        [Fact]
        public void UserStory_UserStoryText_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(UserStory).GetProperty(nameof(UserStory.UserStoryText));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }

        [Fact]
        public void UserStory_ProjectId_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(UserStory).GetProperty(nameof(UserStory.ProjectId));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }
    }
}
