using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class UserRepositoryAsync : GenericRepositoryAsync<User>, IUserRepositoryAsync
{
	public UserRepositoryAsync(IApplicationDbContext context) : base(context)
	{
	}

	public async Task<User?> GetByUsernameAsync(string username)
	{
		return await _dbSet.FirstOrDefaultAsync(user => user.Username == username);
	}
}