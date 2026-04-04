using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Common;

namespace OmniFlow.Application.Interfaces;

public interface IGenericRepositoryAsync<T> where T : BaseEntity
{
	Task<T?> GetByIdAsync(Guid id);
	Task<IReadOnlyList<T>> GetAllAsync();
	Task<PagedResponse<T>> GetPagedAsync(RequestParameter parameter);
	Task<T> AddAsync(T entity);
	Task UpdateAsync(T entity);
	Task DeleteAsync(T entity);
}
