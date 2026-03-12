using Microsoft.EntityFrameworkCore;
using SynTA.Data;

namespace SynTA.Tests.Helpers
{
    /// <summary>
    /// Helper class for creating in-memory database contexts for testing
    /// </summary>
    public static class TestDbContextFactory
    {
        /// <summary>
        /// Creates a new ApplicationDbContext using an in-memory database
        /// </summary>
        /// <param name="databaseName">Unique name for the database instance</param>
        /// <returns>A configured ApplicationDbContext</returns>
        public static ApplicationDbContext CreateInMemoryContext(string? databaseName = null)
        {
            databaseName ??= Guid.NewGuid().ToString();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}
