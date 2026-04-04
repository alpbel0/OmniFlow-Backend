using AutoMapper;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.DTOs.Stops;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Application.Features.Stops.Commands.CreateStop;
using OmniFlow.Application.Features.Stops.Commands.UpdateStop;
using OmniFlow.Application.Features.Trips.Commands.CreateTrip;
using OmniFlow.Application.Features.Trips.Commands.UpdateTrip;
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

        // Trip mappings
        CreateMap<Trip, TripResponse>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Id : Guid.Empty))
            .ForMember(dest => dest.OwnerUsername, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Username : string.Empty))
            .ForMember(dest => dest.OwnerProfilePhotoUrl, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.ProfilePhotoUrl : null));

        CreateMap<Trip, SavedTripResponse>()
            .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SavedAt, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Id : Guid.Empty))
            .ForMember(dest => dest.OwnerUsername, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Username : string.Empty))
            .ForMember(dest => dest.OwnerProfilePhotoUrl, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.ProfilePhotoUrl : null));

        CreateMap<CreateTripCommand, Trip>();
        CreateMap<UpdateTripCommand, Trip>();

        // Stop mappings
        CreateMap<Stop, StopResponse>()
            .ForMember(dest => dest.PlaceName, opt => opt.MapFrom(src => src.Place!.Name))
            .ForMember(dest => dest.PlaceCategory, opt => opt.MapFrom(src => src.Place != null ? src.Place.Category : (Domain.Enums.PlaceCategory?)null))
            .ForMember(dest => dest.PlacePhotoUrl, opt => opt.MapFrom(src => src.Place!.PhotoUrl))
            .ForMember(dest => dest.FallbackPlaceName, opt => opt.MapFrom(src => src.FallbackPlace!.Name))
            .ForMember(dest => dest.FallbackPlaceCategory, opt => opt.MapFrom(src => src.FallbackPlace != null ? src.FallbackPlace.Category : (Domain.Enums.PlaceCategory?)null));

        CreateMap<CreateStopCommand, Stop>();
        CreateMap<UpdateStopCommand, Stop>();
    }
}
