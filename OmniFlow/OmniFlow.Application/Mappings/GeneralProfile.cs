using AutoMapper;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Mappings;

/// <summary>
/// Base AutoMapper profile for the Application layer.
/// Entity → DTO mappings will be added here in Phase 2.
/// </summary>
public class GeneralProfile : Profile
{
    public GeneralProfile()
    {
        // Place mappings
        CreateMap<Place, PlaceResponse>();
        CreateMap<CreatePlaceCommand, Place>();
    }
}
