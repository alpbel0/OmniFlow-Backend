using AutoMapper;
using MediatR;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Features.Places.Commands.CreatePlace;

public class CreatePlaceCommandHandler : IRequestHandler<CreatePlaceCommand, Guid>
{
    private readonly IPlaceRepositoryAsync _placeRepository;
    private readonly IMapper _mapper;

    public CreatePlaceCommandHandler(IPlaceRepositoryAsync placeRepository, IMapper mapper)
    {
        _placeRepository = placeRepository;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreatePlaceCommand request, CancellationToken cancellationToken)
    {
        var place = _mapper.Map<Place>(request);
        place.IsActive = true;

        await _placeRepository.AddAsync(place);

        return place.Id;
    }
}