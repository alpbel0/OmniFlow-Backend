using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Common;

namespace OmniFlow.Infrastructure.Repositories;

public class GenericRepositoryAsync<T> : IGenericRepositoryAsync<T> where T : class
{
	protected readonly IApplicationDbContext _context;
	protected readonly DbSet<T> _dbSet;

	public GenericRepositoryAsync(IApplicationDbContext context)
	{
		_context = context;
		_dbSet = context.Set<T>();
	}

	public virtual async Task<T?> GetByIdAsync(Guid id)
	{
		return await _dbSet.FindAsync(id);
	}

	public virtual async Task<IReadOnlyList<T>> GetAllAsync()
	{
		return await _dbSet.ToListAsync();
	}

	public virtual async Task<PagedResponse<T>> GetPagedAsync(RequestParameter parameter)
	{
		var query = _dbSet.AsQueryable();
		var totalCount = await query.CountAsync();
		var items = await query
			.Skip((parameter.PageNumber - 1) * parameter.PageSize)
			.Take(parameter.PageSize)
			.ToListAsync();
		return new PagedResponse<T>(items, parameter.PageNumber, parameter.PageSize, totalCount);
	}

	public virtual async Task<T> AddAsync(T entity)
	{
		await _dbSet.AddAsync(entity);
		await _context.SaveChangesAsync();
		return entity;
	}

	public virtual async Task UpdateAsync(T entity)
	{
		_dbSet.Update(entity);
		await _context.SaveChangesAsync();
	}

	public virtual async Task DeleteAsync(T entity)
	{
		// Soft-Delete vs Hard-Delete control
		if (entity is AuditableBaseEntity auditableEntity)
		{
			// Soft-delete: set DeletedAt and update
			auditableEntity.DeletedAt = DateTime.UtcNow;
			_dbSet.Update(entity);
		}
		else
		{
			// Hard-delete: remove from database (e.g., Place entity)
			_dbSet.Remove(entity);
		}
		await _context.SaveChangesAsync();
	}
}