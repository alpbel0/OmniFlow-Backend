using MediatR;
using OmniFlow.Application.DTOs.Places;

namespace OmniFlow.Application.Features.Places.Queries.GetPlaceById;

public class GetPlaceByIdQuery : IRequest<PlaceResponse>
{
    public Guid Id { get; set; }

    public GetPlaceByIdQuery(Guid id)
    {
        Id = id;
    }
}