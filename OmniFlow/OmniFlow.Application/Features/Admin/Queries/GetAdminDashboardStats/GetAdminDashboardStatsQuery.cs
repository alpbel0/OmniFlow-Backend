using MediatR;
using OmniFlow.Application.DTOs.Admin;

namespace OmniFlow.Application.Features.Admin.Queries.GetAdminDashboardStats;

public sealed class GetAdminDashboardStatsQuery : IRequest<AdminDashboardStatsResponse>;
