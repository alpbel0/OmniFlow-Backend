using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Repositories;

public class PlaceRepositoryAsync : GenericRepositoryAsync<Place>, IPlaceRepositoryAsync
{
    public PlaceRepositoryAsync(IApplicationDbContext context) : base(context)
    {
    }

    public async Task<PagedResponse<Place>> GetByCityAsync(string city, RequestParameter parameter)
    {
        var query = _dbSet
            .Where(p => p.IsActive)
            .Where(p => p.City.ToLower() == city.ToLower())
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((parameter.PageNumber - 1) * parameter.PageSize)
            .Take(parameter.PageSize)
            .ToListAsync();

        return new PagedResponse<Place>(items, parameter.PageNumber, parameter.PageSize, totalCount);
    }

    public async Task<PagedResponse<Place>> GetByCategoryAsync(PlaceCategory category, RequestParameter parameter)
    {
        var query = _dbSet
            .Where(p => p.IsActive)
            .Where(p => p.Category == category)
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((parameter.PageNumber - 1) * parameter.PageSize)
            .Take(parameter.PageSize)
            .ToListAsync();

        return new PagedResponse<Place>(items, parameter.PageNumber, parameter.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<Place>> GetByCityAndBudgetTierAsync(string city, BudgetTier budgetTier)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Where(p => p.City.ToLower() == city.ToLower())
            .Where(p => p.BudgetTiers.Contains(budgetTier))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}