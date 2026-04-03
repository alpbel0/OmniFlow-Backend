using MediatR;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Wrappers;

namespace OmniFlow.Application.Features.Places.Queries.GetPlacesByCity;

public class GetPlacesByCityQuery : IRequest<PagedResponse<PlaceResponse>>
{
    public string City { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}