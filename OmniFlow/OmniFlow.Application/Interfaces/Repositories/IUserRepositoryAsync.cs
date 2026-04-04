using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Interfaces.Repositories;

public interface IUserRepositoryAsync : IGenericRepositoryAsync<User>
{
	Task<User?> GetByUsernameAsync(string username);
}
