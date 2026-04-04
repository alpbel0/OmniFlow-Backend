using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.DTOs.Notifications;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Wrappers;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Infrastructure.Repositories;

public class NotificationRepositoryAsync : INotificationRepositoryAsync
{
	private readonly IApplicationDbContext _context;
	private readonly DbSet<Notification> _dbSet;

	public NotificationRepositoryAsync(IApplicationDbContext context)
	{
		_context = context;
		_dbSet = context.Set<Notification>();
	}

	public async Task<PagedResponse<NotificationResponse>> GetByUserAsync(
		Guid userId,
		bool? isRead,
		int pageNumber,
		int pageSize,
		CancellationToken cancellationToken = default)
	{
		var normalizedPageNumber = pageNumber > 0 ? pageNumber : 1;
		var normalizedPageSize = pageSize > 0 ? pageSize : 10;

		var query = _dbSet
			.AsNoTracking()
			.Include(notification => notification.Actor)
			.Where(notification => notification.UserId == userId);

		if (isRead.HasValue)
		{
			query = query.Where(notification => notification.IsRead == isRead.Value);
		}

		query = query.OrderByDescending(notification => notification.CreatedAt);

		var totalCount = await query.CountAsync(cancellationToken);
		var data = await query
			.Skip((normalizedPageNumber - 1) * normalizedPageSize)
			.Take(normalizedPageSize)
			.Select(notification => new NotificationResponse
			{
				Id = notification.Id,
				Type = notification.NotificationType,
				TargetId = notification.TargetId,
				TargetType = notification.TargetType,
				IsRead = notification.IsRead,
				ReadAt = notification.ReadAt,
				CreatedAt = notification.CreatedAt,
				ActorUsername = notification.Actor != null ? notification.Actor.Username : null,
				ActorProfilePhotoUrl = notification.Actor != null ? notification.Actor.ProfilePhotoUrl : null
			})
			.ToListAsync(cancellationToken);

		return new PagedResponse<NotificationResponse>(data, normalizedPageNumber, normalizedPageSize, totalCount);
	}

	public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
	{
		return _dbSet.CountAsync(notification => notification.UserId == userId && !notification.IsRead, cancellationToken);
	}
}
