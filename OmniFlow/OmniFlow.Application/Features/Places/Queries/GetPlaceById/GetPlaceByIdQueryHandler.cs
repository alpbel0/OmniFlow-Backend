using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces.Repositories;

namespace OmniFlow.Application.Features.Places.Queries.GetPlaceById;

public class GetPlaceByIdQueryHandler : IRequestHandler<GetPlaceByIdQuery, PlaceResponse>
{
    private readonly IPlaceRepositoryAsync _placeRepository;
    private readonly IMapper _mapper;

    public GetPlaceByIdQueryHandler(IPlaceRepositoryAsync placeRepository, IMapper mapper)
    {
        _placeRepository = placeRepository;
        _mapper = mapper;
    }

    public async Task<PlaceResponse> Handle(GetPlaceByIdQuery request, CancellationToken cancellationToken)
    {
        var place = await _placeRepository.GetByIdAsync(request.Id);

        if (place == null)
        {
            throw new EntityNotFoundException("Place", request.Id);
        }

        return _mapper.Map<PlaceResponse>(place);
    }
}