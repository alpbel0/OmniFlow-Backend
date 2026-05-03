using AutoMapper;
using OmniFlow.Application.DTOs.Flights;
using OmniFlow.Application.DTOs.Hotels;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.DTOs.Providers;
using OmniFlow.Application.DTOs.TimelineEntries;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Comments.Commands.CreateComment;
using OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;
using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Application.Features.Posts.Commands.CreatePost;
using OmniFlow.Application.Features.Posts.Commands.UpdatePost;
using OmniFlow.Application.Features.Trips.Commands.CreateTrip;
using OmniFlow.Application.Features.Trips.Commands.CreateTripWizard;
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
        CreateMap<Place, ScoredPlaceResponse>();
        CreateMap<CreatePlaceCommand, Place>();

        // Trip mappings
        CreateMap<Trip, TripResponse>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Id : Guid.Empty))
            .ForMember(dest => dest.OwnerUsername, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Username : string.Empty))
            .ForMember(dest => dest.OwnerProfilePhotoUrl, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.ProfilePhotoUrl : null))
            .ForMember(dest => dest.IsUpvoted, opt => opt.Ignore())
            .ForMember(dest => dest.IsSaved, opt => opt.Ignore())
            .ForMember(dest => dest.TimelineSummary, opt => opt.Ignore());

        CreateMap<Trip, FeaturedTripResponse>()
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Id : Guid.Empty))
            .ForMember(dest => dest.OwnerUsername, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Username : string.Empty))
            .ForMember(dest => dest.OwnerProfilePhotoUrl, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.ProfilePhotoUrl : null));

        CreateMap<Trip, SavedTripResponse>()
            .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.SavedAt, opt => opt.Ignore()) // Set manually in handler
            .ForMember(dest => dest.OwnerId, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Id : Guid.Empty))
            .ForMember(dest => dest.OwnerUsername, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.Username : string.Empty))
            .ForMember(dest => dest.OwnerProfilePhotoUrl, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.ProfilePhotoUrl : null))
            .ForMember(dest => dest.TravelStyle, opt => opt.Ignore()) // Trip uses List<TravelStyle>, SavedTripResponse uses single TravelStyle
            .ForMember(dest => dest.City, opt => opt.Ignore())        // Moved to TripDestination
            .ForMember(dest => dest.Country, opt => opt.Ignore())     // Moved to TripDestination
            .ForMember(dest => dest.UserBudget, opt => opt.Ignore()); // No longer exists on Trip

        CreateMap<TripDestination, TripDestinationResponse>();

        CreateMap<CreateTripCommand, Trip>();
        CreateMap<CreateTripWizardCommand, Trip>()
            .ForMember(dest => dest.Destinations, opt => opt.Ignore()); // Handled manually in CreateTripWizardCommandHandler

        CreateMap<Trip, CreateTripWizardResponse>()
            .ForMember(dest => dest.TripId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BudgetMessages, opt => opt.Ignore())
            .ForMember(dest => dest.Destinations, opt => opt.MapFrom(src => src.Destinations.OrderBy(d => d.OrderIndex)));

        CreateMap<UpdateTripCommand, Trip>();

        // TimelineEntry mappings
        CreateMap<TimelineEntry, TimelineEntryResponse>()
            .ForMember(dest => dest.PlaceName, opt => opt.MapFrom(src => src.Place != null ? src.Place.Name : null))
            .ForMember(dest => dest.PlaceLatitude, opt => opt.MapFrom(src => src.Place != null ? src.Place.Latitude : (double?)null))
            .ForMember(dest => dest.PlaceLongitude, opt => opt.MapFrom(src => src.Place != null ? src.Place.Longitude : (double?)null))
            .ForMember(dest => dest.PlacePhotoUrl, opt => opt.MapFrom(src => src.Place != null ? src.Place.PhotoUrl : null));
        CreateMap<CreateTimelineEntryRequest, TimelineEntry>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.OrderIndex, opt => opt.Ignore())
            .ForMember(dest => dest.IsLocked, opt => opt.Ignore())
            .ForMember(dest => dest.BufferMinutes, opt => opt.Ignore())
            .ForMember(dest => dest.VisitedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AddedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

        // Flight mappings
        CreateMap<Flight, FlightResponse>();

        // Hotel mappings
        CreateMap<Hotel, HotelResponse>();

        // Provider mappings
        CreateMap<ProviderFlight, ProviderFlightResponse>()
            .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.SeasonAdjustedPrice, opt => opt.Ignore())
            .ForMember(dest => dest.SeasonMultiplier, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore());

        CreateMap<ProviderHotel, ProviderHotelResponse>()
            .ForMember(dest => dest.BasePricePerNight, opt => opt.MapFrom(src => src.PricePerNight))
            .ForMember(dest => dest.SeasonAdjustedPricePerNight, opt => opt.Ignore())
            .ForMember(dest => dest.SeasonMultiplier, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.NightCount, opt => opt.Ignore())
            .ForMember(dest => dest.Segment, opt => opt.Ignore());

        // Follow mappings
        CreateMap<User, FollowUserResponse>();
        CreateMap<User, UserProfileResponse>()
            .ForMember(dest => dest.IsFollowing, opt => opt.Ignore())
            .ForMember(dest => dest.TripCount, opt => opt.Ignore())
            .ForMember(dest => dest.PostCount, opt => opt.Ignore());

        // Post mappings
        CreateMap<Post, PostResponse>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : string.Empty))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.User != null ? src.User.ProfilePhotoUrl : null))
            .ForMember(dest => dest.KarmaScore, opt => opt.MapFrom(src => src.User != null ? src.User.KarmaScore : 0))
            .ForMember(dest => dest.IsUpvoted, opt => opt.Ignore());

        CreateMap<CreatePostCommand, Post>();
        CreateMap<UpdatePostCommand, Post>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.TripId, opt => opt.Ignore())
            .ForMember(dest => dest.PlaceId, opt => opt.Ignore())
            .ForMember(dest => dest.PostType, opt => opt.Ignore())
            .ForMember(dest => dest.Photos, opt => opt.Ignore())
            .ForMember(dest => dest.AiTags, opt => opt.Ignore())
            .ForMember(dest => dest.LocationLatitude, opt => opt.Ignore())
            .ForMember(dest => dest.LocationLongitude, opt => opt.Ignore())
            .ForMember(dest => dest.City, opt => opt.Ignore())
            .ForMember(dest => dest.Country, opt => opt.Ignore())
            .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.CommentCount, opt => opt.Ignore())
            .ForMember(dest => dest.IsVisible, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Trip, opt => opt.Ignore())
            .ForMember(dest => dest.Place, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Comment mappings
        CreateMap<Comment, CommentResponse>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : string.Empty))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.User != null ? src.User.ProfilePhotoUrl : null))
            .ForMember(dest => dest.KarmaScore, opt => opt.MapFrom(src => src.User != null ? src.User.KarmaScore : 0))
            .ForMember(dest => dest.IsUpvoted, opt => opt.Ignore());

        CreateMap<CommunityTip, TipResponse>()
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User != null ? src.User.Username : string.Empty))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.User != null ? src.User.ProfilePhotoUrl : null))
            .ForMember(dest => dest.KarmaScore, opt => opt.MapFrom(src => src.User != null ? src.User.KarmaScore : 0))
            .ForMember(dest => dest.Place, opt => opt.MapFrom(src => src.Place))
            .ForMember(dest => dest.IsUpvoted, opt => opt.Ignore());

        CreateMap<CreateTipCommand, CommunityTip>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.IsVisible, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Trip, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Place, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateCommentCommand, Comment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.IsVisible, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Post, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.ParentComment, opt => opt.Ignore())
            .ForMember(dest => dest.Replies, opt => opt.Ignore());
    }
}
