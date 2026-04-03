using MediatR;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Places.Queries.GetAllPlaces;

public class GetAllPlacesQuery : IRequest<PagedResponse<PlaceResponse>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}