using SynTA.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace SynTA.Tests.Models
{
    public class CypressScriptModelTests
    {
        [Fact]
        public void CypressScript_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var script = new CypressScript();

            // Assert
            Assert.Equal(0, script.Id);
            Assert.Equal(string.Empty, script.FileName);
            Assert.Equal(string.Empty, script.Content);
            Assert.Null(script.TargetUrl);
            Assert.Equal(0, script.UserStoryId);
            Assert.Null(script.UpdatedAt);
        }

        [Fact]
        public void CypressScript_CreatedAt_DefaultsToUtcNow()
        {
            // Arrange
            var beforeCreation = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var script = new CypressScript();

            // Assert
            Assert.True(script.CreatedAt >= beforeCreation);
            Assert.True(script.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public void CypressScript_CanSetAllProperties()
        {
            // Arrange
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow.AddHours(1);

            // Act
            var script = new CypressScript
            {
                Id = 1,
                FileName = "test.cy.ts",
                Content = "describe('Test', () => {});",
                TargetUrl = "https://example.com",
                UserStoryId = 10,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.Equal(1, script.Id);
            Assert.Equal("test.cy.ts", script.FileName);
            Assert.Equal("describe('Test', () => {});", script.Content);
            Assert.Equal("https://example.com", script.TargetUrl);
            Assert.Equal(10, script.UserStoryId);
            Assert.Equal(createdAt, script.CreatedAt);
            Assert.Equal(updatedAt, script.UpdatedAt);
        }

        [Fact]
        public void CypressScript_FileName_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(CypressScript).GetProperty(nameof(CypressScript.FileName));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }

        [Fact]
        public void CypressScript_FileName_HasMaxLengthOf300()
        {
            // Arrange
            var property = typeof(CypressScript).GetProperty(nameof(CypressScript.FileName));

            // Act
            var maxLengthAttr = property?.GetCustomAttributes(typeof(MaxLengthAttribute), false)
                .Cast<MaxLengthAttribute>().FirstOrDefault();

            // Assert
            Assert.NotNull(maxLengthAttr);
            Assert.Equal(300, maxLengthAttr.Length);
        }

        [Fact]
        public void CypressScript_Content_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(CypressScript).GetProperty(nameof(CypressScript.Content));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }

        [Fact]
        public void CypressScript_UserStoryId_HasRequiredAttribute()
        {
            // Arrange
            var property = typeof(CypressScript).GetProperty(nameof(CypressScript.UserStoryId));

            // Act
            var requiredAttr = property?.GetCustomAttributes(typeof(RequiredAttribute), false);

            // Assert
            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);
        }
    }
}
