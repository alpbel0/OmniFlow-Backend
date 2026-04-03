using AutoMapper;
using Moq;
using OmniFlow.Application.Features.Users.Queries.GetSavedTrips;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.UnitTests.Features.Users;

public class GetSavedTripsQueryHandlerTests
{
    // GetSavedTripsQueryHandler tests are covered by integration tests:
    // - GetSavedTrips_WithValidToken_Returns200
    // - GetSavedTrips_ReturnsCorrectPagination
    // No need for unit tests with complex EF Core async mocking.
}