using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Hotels;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Features.Hotels.Queries.GetHotelsByTrip;

public class GetHotelsByTripQueryHandler : IRequestHandler<GetHotelsByTripQuery, HotelsByTripViewModel>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IHotelRepositoryAsync _hotelRepository;
    private readonly IAuthenticatedUserService _authenticatedUserService;
    private readonly IMapper _mapper;

    public GetHotelsByTripQueryHandler(
        ITripRepositoryAsync tripRepository,
        IHotelRepositoryAsync hotelRepository,
        IAuthenticatedUserService authenticatedUserService,
        IMapper mapper)
    {
        _tripRepository = tripRepository;
        _hotelRepository = hotelRepository;
        _authenticatedUserService = authenticatedUserService;
        _mapper = mapper;
    }

    public async Task<HotelsByTripViewModel> Handle(GetHotelsByTripQuery request, CancellationToken cancellationToken)
    {
        // 1. Get trip with owner
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        // 2. Authorization: Published trips are public, Draft/Archived are owner-only
        var currentUserId = Guid.Parse(_authenticatedUserService.UserId);
        if (trip.Status != TripStatus.Published && trip.OwnerId != currentUserId)
        {
            throw new ForbiddenException("You can only view hotels from published trips or your own trips.");
        }

        // 3. Get all hotels for trip (sorted by CheckIn)
        var hotels = await _hotelRepository.GetByTripAsync(request.TripId);

        // 4. Map to HotelResponse
        var viewModel = new HotelsByTripViewModel
        {
            Hotels = _mapper.Map<IReadOnlyList<HotelResponse>>(hotels)
        };

        return viewModel;
    }
}