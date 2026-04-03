using Microsoft.EntityFrameworkCore;
using Moq;

namespace OmniFlow.UnitTests;

/// <summary>
/// Helper for mocking DbSet in unit tests.
/// </summary>
internal static class MockDbSetHelper
{
    public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var query = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(query.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(query.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(query.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(query.GetEnumerator());
        return mockSet;
    }
}