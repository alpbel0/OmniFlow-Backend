using AutoMapper;
using MediatR;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Interfaces.Repositories;
using OmniFlow.Application.Parameters;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Places.Queries.GetAllPlaces;

public class GetAllPlacesQueryHandler : IRequestHandler<GetAllPlacesQuery, PagedResponse<PlaceResponse>>
{
    private readonly IPlaceRepositoryAsync _placeRepository;
    private readonly IMapper _mapper;

    public GetAllPlacesQueryHandler(IPlaceRepositoryAsync placeRepository, IMapper mapper)
    {
        _placeRepository = placeRepository;
        _mapper = mapper;
    }

    public async Task<PagedResponse<PlaceResponse>> Handle(GetAllPlacesQuery request, CancellationToken cancellationToken)
    {
        var parameter = new RequestParameter
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var pagedPlaces = await _placeRepository.GetPagedAsync(parameter);
        var mappedData = _mapper.Map<IReadOnlyList<PlaceResponse>>(pagedPlaces.Data);

        return new PagedResponse<PlaceResponse>(
            mappedData,
            pagedPlaces.PageNumber,
            pagedPlaces.PageSize,
            pagedPlaces.TotalCount
        );
    }
}