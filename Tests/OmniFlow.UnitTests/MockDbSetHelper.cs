using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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

    public static Mock<DbSet<T>> CreateAsyncMockDbSet<T>(List<T> data) where T : class
    {
        var query = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        // Create async query provider
        var asyncProvider = new TestAsyncQueryProvider<T>(query.Provider);

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncProvider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(query.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(query.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(query.GetEnumerator());

        // Setup IAsyncEnumerable
        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }
}

internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetGenericArguments()[0];
        var queryType = typeof(TestAsyncEnumerable<>).MakeGenericType(elementType);
        return (IQueryable)Activator.CreateInstance(queryType, expression)!;
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression)!;
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        // Wrap synchronous query results in Task<T> when EF asks for async execution.
        var result = _inner.Execute(expression);

        var resultType = typeof(TResult);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = resultType.GenericTypeArguments[0];
            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(Task.FromResult))
                .MakeGenericMethod(taskResultType);

            return (TResult)fromResultMethod.Invoke(null, [result])!;
        }

        return (TResult)result!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    internal TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
    {
        return ValueTask.FromResult(_inner.MoveNext());
    }
}