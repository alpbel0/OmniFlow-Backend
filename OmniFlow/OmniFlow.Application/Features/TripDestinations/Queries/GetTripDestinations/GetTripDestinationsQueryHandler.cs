using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.TripDestinations.Queries.GetTripDestinations;

public class GetTripDestinationsQueryHandler : IRequestHandler<GetTripDestinationsQuery, IReadOnlyList<TripDestinationResponse>>
{
    private readonly ITripDestinationRepositoryAsync _tripDestinationRepository;
    private readonly IMapper _mapper;

    public GetTripDestinationsQueryHandler(
        ITripDestinationRepositoryAsync tripDestinationRepository,
        IMapper mapper)
    {
        _tripDestinationRepository = tripDestinationRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<TripDestinationResponse>> Handle(GetTripDestinationsQuery request, CancellationToken cancellationToken)
    {
        var destinations = await _tripDestinationRepository.GetByTripAsync(request.TripId);
        return _mapper.Map<IReadOnlyList<TripDestinationResponse>>(destinations);
    }
}
