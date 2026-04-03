using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Trips.Queries.GetTripById;

public class GetTripByIdQueryHandler : IRequestHandler<GetTripByIdQuery, TripResponse>
{
    private readonly ITripRepositoryAsync _tripRepository;
    private readonly IMapper _mapper;

    public GetTripByIdQueryHandler(ITripRepositoryAsync tripRepository, IMapper mapper)
    {
        _tripRepository = tripRepository;
        _mapper = mapper;
    }

    public async Task<TripResponse> Handle(GetTripByIdQuery request, CancellationToken cancellationToken)
    {
        // GetByIdWithOwnerAsync kullanıyoruz - Owner include olmadan mapping patlar!
        var trip = await _tripRepository.GetByIdWithOwnerAsync(request.TripId);

        if (trip == null)
        {
            throw new EntityNotFoundException("Trip", request.TripId);
        }

        return _mapper.Map<TripResponse>(trip);
    }
}