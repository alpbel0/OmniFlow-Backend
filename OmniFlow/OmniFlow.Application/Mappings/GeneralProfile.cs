using AutoMapper;
using OmniFlow.Application.DTOs.Flights;
using OmniFlow.Application.DTOs.Hotels;
using OmniFlow.Application.DTOs.Comments;
using OmniFlow.Application.DTOs.CommunityTips;
using OmniFlow.Application.DTOs.Follows;
using OmniFlow.Application.DTOs.Users;
using OmniFlow.Application.DTOs.Places;
using OmniFlow.Application.DTOs.Posts;
using OmniFlow.Application.DTOs.Trips;
using OmniFlow.Application.Features.Comments.Commands.CreateComment;
using OmniFlow.Application.Features.CommunityTips.Commands.CreateTip;
using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Application.Features.Posts.Commands.CreatePost;
using OmniFlow.Application.Features.Posts.Commands.UpdatePost;
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
            .ForMember(dest => dest.OwnerProfilePhotoUrl, opt => opt.MapFrom(src => src.Owner != null ? src.Owner.ProfilePhotoUrl : null))
            .ForMember(dest => dest.IsUpvoted, opt => opt.Ignore())
            .ForMember(dest => dest.IsSaved, opt => opt.Ignore());

        CreateMap<Trip, FeaturedTripResponse>()
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

        // Flight mappings
        CreateMap<Flight, FlightResponse>();

        // Hotel mappings
        CreateMap<Hotel, HotelResponse>();

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
