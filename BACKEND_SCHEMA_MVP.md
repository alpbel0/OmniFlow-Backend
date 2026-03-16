# OmniFlow Backend — Klasör Şeması (MVP / CSE332 Aşaması)

```
Backend/
│
├── OmniFlow/
│   ├── OmniFlow.slnx
│   │
│   ├── OmniFlow.Domain/
│   │   ├── Common/
│   │   │   ├── AuditableBaseEntity.cs
│   │   │   └── BaseEntity.cs
│   │   │
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Trip.cs
│   │   │   ├── Place.cs
│   │   │   ├── Stop.cs
│   │   │   ├── Flight.cs
│   │   │   ├── Hotel.cs
│   │   │   ├── Post.cs
│   │   │   ├── Comment.cs
│   │   │   ├── CommunityTip.cs
│   │   │   ├── Follow.cs
│   │   │   ├── PostUpvote.cs
│   │   │   ├── CommentUpvote.cs
│   │   │   ├── TipUpvote.cs
│   │   │   ├── TripUpvote.cs
│   │   │   ├── SavedTrip.cs
│   │   │   ├── Notification.cs
│   │   │   ├── KarmaEvent.cs
│   │   │   └── RefreshToken.cs
│   │   │
│   │   ├── Enums/
│   │   │   ├── Roles.cs
│   │   │   ├── BudgetTier.cs
│   │   │   ├── TravelStyle.cs
│   │   │   ├── TripStatus.cs
│   │   │   ├── PlaceCategory.cs
│   │   │   ├── TransportMode.cs
│   │   │   ├── StopAddedBy.cs
│   │   │   ├── CabinClass.cs
│   │   │   ├── FlightStatus.cs
│   │   │   ├── FlightDirection.cs
│   │   │   ├── FlightDataSource.cs
│   │   │   ├── RoomType.cs
│   │   │   ├── CancellationPolicy.cs
│   │   │   ├── HotelStatus.cs
│   │   │   ├── HotelDataSource.cs
│   │   │   ├── PostType.cs
│   │   │   ├── NotificationType.cs
│   │   │   ├── NotificationTargetType.cs
│   │   │   ├── KarmaEventType.cs
│   │   │   └── KarmaSourceType.cs
│   │   │
│   │   ├── Exceptions/
│   │   │   ├── DuplicateUpvoteException.cs
│   │   │   ├── SelfFollowException.cs
│   │   │   └── SelfForkException.cs
│   │   │
│   │   └── OmniFlow.Domain.csproj
│   │
│   ├── OmniFlow.Application/
│   │   ├── Behaviours/
│   │   │   └── ValidationBehaviour.cs
│   │   │
│   │   ├── Exceptions/
│   │   │   ├── ApiException.cs
│   │   │   ├── EntityNotFoundException.cs
│   │   │   ├── ForbiddenException.cs
│   │   │   └── ValidationException.cs
│   │   │
│   │   ├── DTOs/
│   │   │   ├── Account/
│   │   │   │   ├── AuthenticationRequest.cs
│   │   │   │   ├── AuthenticationResponse.cs
│   │   │   │   ├── ForgotPasswordRequest.cs
│   │   │   │   ├── RefreshTokenRequest.cs
│   │   │   │   └── RegisterRequest.cs
│   │   │   │
│   │   │   ├── Email/
│   │   │   │   └── EmailRequest.cs
│   │   │   │
│   │   │   ├── Trips/
│   │   │   │   ├── CreateTripRequest.cs
│   │   │   │   ├── UpdateTripRequest.cs
│   │   │   │   └── TripResponse.cs
│   │   │   │
│   │   │   ├── Stops/
│   │   │   │   ├── CreateStopRequest.cs
│   │   │   │   ├── UpdateStopRequest.cs
│   │   │   │   ├── ReorderStopRequest.cs
│   │   │   │   └── StopResponse.cs
│   │   │   │
│   │   │   ├── Places/
│   │   │   │   └── PlaceResponse.cs
│   │   │   │
│   │   │   ├── Flights/
│   │   │   │   ├── SelectFlightRequest.cs
│   │   │   │   └── FlightResponse.cs
│   │   │   │
│   │   │   ├── Hotels/
│   │   │   │   ├── SelectHotelRequest.cs
│   │   │   │   └── HotelResponse.cs
│   │   │   │
│   │   │   ├── Posts/
│   │   │   │   ├── CreatePostRequest.cs
│   │   │   │   ├── UpdatePostRequest.cs
│   │   │   │   └── PostResponse.cs
│   │   │   │
│   │   │   ├── Comments/
│   │   │   │   ├── CreateCommentRequest.cs
│   │   │   │   └── CommentResponse.cs
│   │   │   │
│   │   │   ├── CommunityTips/
│   │   │   │   ├── CreateTipRequest.cs
│   │   │   │   └── TipResponse.cs
│   │   │   │
│   │   │   ├── Follows/
│   │   │   │   └── FollowResponse.cs
│   │   │   │
│   │   │   ├── Notifications/
│   │   │   │   └── NotificationResponse.cs
│   │   │   │
│   │   │   ├── Karma/
│   │   │   │   └── KarmaEventResponse.cs
│   │   │   │
│   │   │   └── Users/
│   │   │       ├── UserProfileResponse.cs
│   │   │       └── UpdateProfileRequest.cs
│   │   │
│   │   ├── Features/
│   │   │   ├── Trips/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateTrip/
│   │   │   │   │   │   ├── CreateTripCommand.cs
│   │   │   │   │   │   └── CreateTripCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── UpdateTrip/
│   │   │   │   │   │   ├── UpdateTripCommand.cs
│   │   │   │   │   │   └── UpdateTripCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── DeleteTrip/
│   │   │   │   │   │   └── DeleteTripCommand.cs
│   │   │   │   │   │
│   │   │   │   │   ├── PublishTrip/
│   │   │   │   │   │   └── PublishTripCommand.cs
│   │   │   │   │   │
│   │   │   │   │   ├── ArchiveTrip/
│   │   │   │   │   │   └── ArchiveTripCommand.cs
│   │   │   │   │   │
│   │   │   │   │   ├── ForkTrip/
│   │   │   │   │   │   └── ForkTripCommand.cs
│   │   │   │   │   │
│   │   │   │   │   ├── GenerateTimeline/
│   │   │   │   │   │   └── GenerateTimelineCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── UpvoteTrip/
│   │   │   │   │       └── UpvoteTripCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       ├── GetTripById/
│   │   │   │       │   └── GetTripByIdQuery.cs
│   │   │   │       │
│   │   │   │       ├── GetMyTrips/
│   │   │   │       │   ├── GetMyTripsParameter.cs
│   │   │   │       │   ├── GetMyTripsQuery.cs
│   │   │   │       │   └── GetMyTripsViewModel.cs
│   │   │   │       │
│   │   │   │       └── ExploreTrips/
│   │   │   │           ├── ExploreTripsParameter.cs
│   │   │   │           ├── ExploreTripsQuery.cs
│   │   │   │           └── ExploreTripsViewModel.cs
│   │   │   │
│   │   │   ├── Stops/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateStop/
│   │   │   │   │   │   ├── CreateStopCommand.cs
│   │   │   │   │   │   └── CreateStopCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── UpdateStop/
│   │   │   │   │   │   ├── UpdateStopCommand.cs
│   │   │   │   │   │   └── UpdateStopCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── DeleteStop/
│   │   │   │   │   │   └── DeleteStopCommand.cs
│   │   │   │   │   │
│   │   │   │   │   ├── ReorderStops/
│   │   │   │   │   │   └── ReorderStopsCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── MarkStopVisited/
│   │   │   │   │       └── MarkStopVisitedCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       └── GetStopsByTrip/
│   │   │   │           ├── GetStopsByTripQuery.cs
│   │   │   │           └── GetStopsByTripViewModel.cs
│   │   │   │
│   │   │   ├── Places/
│   │   │   │   ├── Commands/
│   │   │   │   │   └── CreatePlace/
│   │   │   │   │       ├── CreatePlaceCommand.cs
│   │   │   │   │       └── CreatePlaceCommandValidator.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       ├── GetAllPlaces/
│   │   │   │       │   ├── GetAllPlacesParameter.cs
│   │   │   │       │   ├── GetAllPlacesQuery.cs
│   │   │   │       │   └── GetAllPlacesViewModel.cs
│   │   │   │       │
│   │   │   │       ├── GetPlaceById/
│   │   │   │       │   └── GetPlaceByIdQuery.cs
│   │   │   │       │
│   │   │   │       └── GetPlacesByCity/
│   │   │   │           ├── GetPlacesByCityParameter.cs
│   │   │   │           └── GetPlacesByCityQuery.cs
│   │   │   │
│   │   │   ├── Flights/
│   │   │   │   ├── Commands/
│   │   │   │   │   └── SelectFlight/
│   │   │   │   │       └── SelectFlightCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       └── GetFlightsByTrip/
│   │   │   │           └── GetFlightsByTripQuery.cs
│   │   │   │
│   │   │   ├── Hotels/
│   │   │   │   ├── Commands/
│   │   │   │   │   └── SelectHotel/
│   │   │   │   │       └── SelectHotelCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       └── GetHotelsByTrip/
│   │   │   │           └── GetHotelsByTripQuery.cs
│   │   │   │
│   │   │   ├── Posts/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreatePost/
│   │   │   │   │   │   ├── CreatePostCommand.cs
│   │   │   │   │   │   └── CreatePostCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── UpdatePost/
│   │   │   │   │   │   └── UpdatePostCommand.cs
│   │   │   │   │   │
│   │   │   │   │   ├── DeletePost/
│   │   │   │   │   │   └── DeletePostCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── UpvotePost/
│   │   │   │   │       └── UpvotePostCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       ├── GetFeed/
│   │   │   │       │   ├── GetFeedParameter.cs
│   │   │   │       │   ├── GetFeedQuery.cs
│   │   │   │       │   └── GetFeedViewModel.cs
│   │   │   │       │
│   │   │   │       └── GetPostById/
│   │   │   │           └── GetPostByIdQuery.cs
│   │   │   │
│   │   │   ├── Comments/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateComment/
│   │   │   │   │   │   ├── CreateCommentCommand.cs
│   │   │   │   │   │   └── CreateCommentCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── DeleteComment/
│   │   │   │   │   │   └── DeleteCommentCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── UpvoteComment/
│   │   │   │   │       └── UpvoteCommentCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       └── GetCommentsByPost/
│   │   │   │           ├── GetCommentsByPostQuery.cs
│   │   │   │           └── GetCommentsByPostViewModel.cs
│   │   │   │
│   │   │   ├── CommunityTips/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateTip/
│   │   │   │   │   │   ├── CreateTipCommand.cs
│   │   │   │   │   │   └── CreateTipCommandValidator.cs
│   │   │   │   │   │
│   │   │   │   │   ├── DeleteTip/
│   │   │   │   │   │   └── DeleteTipCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── UpvoteTip/
│   │   │   │   │       └── UpvoteTipCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       └── GetTipsByTrip/
│   │   │   │           ├── GetTipsByTripQuery.cs
│   │   │   │           └── GetTipsByTripViewModel.cs
│   │   │   │
│   │   │   ├── Follows/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── FollowUser/
│   │   │   │   │   │   └── FollowUserCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── UnfollowUser/
│   │   │   │   │       └── UnfollowUserCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       ├── GetFollowers/
│   │   │   │       │   ├── GetFollowersParameter.cs
│   │   │   │       │   └── GetFollowersQuery.cs
│   │   │   │       │
│   │   │   │       └── GetFollowing/
│   │   │   │           ├── GetFollowingParameter.cs
│   │   │   │           └── GetFollowingQuery.cs
│   │   │   │
│   │   │   ├── Notifications/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── MarkAsRead/
│   │   │   │   │   │   └── MarkAsReadCommand.cs
│   │   │   │   │   │
│   │   │   │   │   └── MarkAllAsRead/
│   │   │   │   │       └── MarkAllAsReadCommand.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       ├── GetNotifications/
│   │   │   │       │   ├── GetNotificationsParameter.cs
│   │   │   │       │   ├── GetNotificationsQuery.cs
│   │   │   │       │   └── GetNotificationsViewModel.cs
│   │   │   │       │
│   │   │   │       └── GetUnreadCount/
│   │   │   │           └── GetUnreadCountQuery.cs
│   │   │   │
│   │   │   ├── Karma/
│   │   │   │   └── Queries/
│   │   │   │       └── GetKarmaHistory/
│   │   │   │           ├── GetKarmaHistoryParameter.cs
│   │   │   │           ├── GetKarmaHistoryQuery.cs
│   │   │   │           └── GetKarmaHistoryViewModel.cs
│   │   │   │
│   │   │   ├── Users/
│   │   │   │   ├── Commands/
│   │   │   │   │   └── UpdateProfile/
│   │   │   │   │       ├── UpdateProfileCommand.cs
│   │   │   │   │       └── UpdateProfileCommandValidator.cs
│   │   │   │   │
│   │   │   │   └── Queries/
│   │   │   │       ├── GetUserProfile/
│   │   │   │       │   └── GetUserProfileQuery.cs
│   │   │   │       │
│   │   │   │       └── GetSavedTrips/
│   │   │   │           ├── GetSavedTripsParameter.cs
│   │   │   │           └── GetSavedTripsQuery.cs
│   │   │   │
│   │   │   └── SavedTrips/
│   │   │       └── Commands/
│   │   │           ├── SaveTrip/
│   │   │           │   └── SaveTripCommand.cs
│   │   │           │
│   │   │           └── UnsaveTrip/
│   │   │               └── UnsaveTripCommand.cs
│   │   │
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IUserRepositoryAsync.cs
│   │   │   │   ├── ITripRepositoryAsync.cs
│   │   │   │   ├── IPlaceRepositoryAsync.cs
│   │   │   │   ├── IStopRepositoryAsync.cs
│   │   │   │   ├── IFlightRepositoryAsync.cs
│   │   │   │   ├── IHotelRepositoryAsync.cs
│   │   │   │   ├── IPostRepositoryAsync.cs
│   │   │   │   ├── ICommentRepositoryAsync.cs
│   │   │   │   ├── ICommunityTipRepositoryAsync.cs
│   │   │   │   ├── IFollowRepositoryAsync.cs
│   │   │   │   ├── INotificationRepositoryAsync.cs
│   │   │   │   └── IKarmaEventRepositoryAsync.cs
│   │   │   │
│   │   │   ├── IApplicationDbContext.cs
│   │   │   ├── IAccountService.cs
│   │   │   ├── IAuthenticatedUserService.cs
│   │   │   ├── IDateTimeService.cs
│   │   │   ├── IEmailService.cs
│   │   │   ├── IAiTimelineService.cs
│   │   │   ├── IAiFallbackService.cs
│   │   │   ├── IKarmaService.cs
│   │   │   ├── INotificationService.cs
│   │   │   └── IGenericRepositoryAsync.cs
│   │   │
│   │   ├── Services/
│   │   │   ├── KarmaService.cs
│   │   │   └── NotificationService.cs
│   │   │
│   │   ├── Mappings/
│   │   │   └── GeneralProfile.cs
│   │   │
│   │   ├── Parameters/
│   │   │   └── RequestParameter.cs
│   │   │
│   │   ├── Settings/
│   │   │   ├── JWTSettings.cs
│   │   │   ├── MailSettings.cs
│   │   │   ├── OpenAISettings.cs
│   │   │   └── GoogleMapsSettings.cs
│   │   │
│   │   ├── Wrappers/
│   │   │   ├── ErrorResponse.cs
│   │   │   └── PagedResponse.cs
│   │   │
│   │   ├── OmniFlow.Application.csproj          ← references: OmniFlow.Domain
│   │   └── ServiceExtensions.cs
│   │
│   ├── OmniFlow.Infrastructure/
│   │   ├── Contexts/
│   │   │   └── ApplicationDbContext.cs
│   │   │
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── TripConfiguration.cs
│   │   │   ├── PlaceConfiguration.cs
│   │   │   ├── StopConfiguration.cs
│   │   │   ├── FlightConfiguration.cs
│   │   │   ├── HotelConfiguration.cs
│   │   │   ├── PostConfiguration.cs
│   │   │   ├── CommentConfiguration.cs
│   │   │   ├── CommunityTipConfiguration.cs
│   │   │   ├── FollowConfiguration.cs
│   │   │   ├── PostUpvoteConfiguration.cs
│   │   │   ├── CommentUpvoteConfiguration.cs
│   │   │   ├── TipUpvoteConfiguration.cs
│   │   │   ├── TripUpvoteConfiguration.cs
│   │   │   ├── SavedTripConfiguration.cs
│   │   │   ├── NotificationConfiguration.cs
│   │   │   ├── KarmaEventConfiguration.cs
│   │   │   └── RefreshTokenConfiguration.cs
│   │   │
│   │   ├── Helpers/
│   │   │   ├── AuthenticationHelper.cs
│   │   │   └── IpHelper.cs
│   │   │
│   │   ├── Migrations/
│   │   │   └── .gitkeep
│   │   │
│   │   ├── Models/
│   │   │   └── ApplicationUser.cs
│   │   │
│   │   ├── Repositories/
│   │   │   ├── GenericRepositoryAsync.cs
│   │   │   ├── UserRepositoryAsync.cs
│   │   │   ├── TripRepositoryAsync.cs
│   │   │   ├── PlaceRepositoryAsync.cs
│   │   │   ├── StopRepositoryAsync.cs
│   │   │   ├── FlightRepositoryAsync.cs
│   │   │   ├── HotelRepositoryAsync.cs
│   │   │   ├── PostRepositoryAsync.cs
│   │   │   ├── CommentRepositoryAsync.cs
│   │   │   ├── CommunityTipRepositoryAsync.cs
│   │   │   ├── FollowRepositoryAsync.cs
│   │   │   ├── NotificationRepositoryAsync.cs
│   │   │   └── KarmaEventRepositoryAsync.cs
│   │   │
│   │   ├── Seeds/
│   │   │   ├── DefaultBasicUser.cs
│   │   │   ├── DefaultRoles.cs
│   │   │   └── DefaultSuperAdmin.cs
│   │   │
│   │   ├── Services/
│   │   │   ├── AccountService.cs
│   │   │   ├── DateTimeService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── AiTimelineService.cs
│   │   │   └── AiFallbackService.cs
│   │   │
│   │   ├── OmniFlow.Infrastructure.csproj        ← references: OmniFlow.Application
│   │   └── ServiceRegistration.cs
│   │
│   └── OmniFlow.WebApi/
│       ├── Controllers/
│       │   ├── v1/
│       │   │   ├── TripsController.cs
│       │   │   ├── StopsController.cs
│       │   │   ├── PlacesController.cs
│       │   │   ├── FlightsController.cs
│       │   │   ├── HotelsController.cs
│       │   │   ├── PostsController.cs
│       │   │   ├── CommentsController.cs
│       │   │   ├── CommunityTipsController.cs
│       │   │   ├── FollowsController.cs
│       │   │   ├── NotificationsController.cs
│       │   │   ├── KarmaController.cs
│       │   │   ├── UsersController.cs
│       │   │   └── SavedTripsController.cs
│       │   │
│       │   ├── AccountController.cs
│       │   ├── BaseApiController.cs
│       │   └── MetaController.cs
│       │
│       ├── Extensions/
│       │   ├── AppExtensions.cs
│       │   └── ServiceExtensions.cs
│       │
│       ├── Middlewares/
│       │   └── ErrorHandlerMiddleware.cs
│       │
│       ├── Models/
│       │   └── Metadata.cs
│       │
│       ├── Properties/
│       │   └── launchSettings.json
│       │
│       ├── Services/
│       │   └── AuthenticatedUserService.cs
│       │
│       ├── appsettings.Development.json
│       ├── appsettings.json
│       ├── OmniFlow.WebApi.csproj                ← references: OmniFlow.Application, OmniFlow.Infrastructure
│       ├── OmniFlow.WebApi.xml
│       └── Program.cs
│
└── Tests/
    ├── OmniFlow.Api.IntegrationTests/
    │   ├── OmniFlow.Api.IntegrationTests.csproj
    │   ├── Setup/
    │   │   ├── CustomWebApplicationFactory.cs
    │   │   └── TestDatabaseSeeder.cs
    │   │
    │   ├── Controllers/
    │   │   ├── AccountControllerTests.cs
    │   │   ├── TripsControllerTests.cs
    │   │   ├── StopsControllerTests.cs
    │   │   ├── PostsControllerTests.cs
    │   │   ├── FollowsControllerTests.cs
    │   │   └── NotificationsControllerTests.cs
    │   │
    │   └── Usings.cs
    │
    ├── OmniFlow.Infrastructure.Tests/
    │   ├── OmniFlow.Infrastructure.Tests.csproj
    │   ├── TripRepositoryTest.cs
    │   ├── StopRepositoryTest.cs
    │   ├── PlaceRepositoryTest.cs
    │   ├── PostRepositoryTest.cs
    │   ├── FollowRepositoryTest.cs
    │   ├── KarmaEventRepositoryTest.cs
    │   └── Usings.cs
    │
    └── OmniFlow.UnitTests/
        ├── OmniFlow.UnitTests.csproj
        ├── Trips/
        │   ├── CreateTripCommandTests.cs
        │   ├── ForkTripCommandTests.cs
        │   ├── UpvoteTripCommandTests.cs
        │   └── ExploreTripQueryTests.cs
        │
        ├── Stops/
        │   ├── CreateStopCommandTests.cs
        │   ├── ReorderStopsCommandTests.cs
        │   └── MarkStopVisitedCommandTests.cs
        │
        ├── Posts/
        │   ├── CreatePostCommandTests.cs
        │   └── UpvotePostCommandTests.cs
        │
        ├── Comments/
        │   └── CreateCommentCommandTests.cs
        │
        ├── Follows/
        │   ├── FollowUserCommandTests.cs
        │   └── UnfollowUserCommandTests.cs
        │
        ├── Karma/
        │   └── KarmaServiceTests.cs
        │
        └── Usings.cs
```

## Proje Referans Zinciri

```
OmniFlow.Domain            ← Hiçbir projeye referansı yoktur (bağımsız)
    ↑
OmniFlow.Application       ← Sadece Domain'i referans alır
    ↑
OmniFlow.Infrastructure    ← Application'ı referans alır (dolaylı olarak Domain'e erişir)
    ↑
OmniFlow.WebApi            ← Application + Infrastructure'ı referans alır
```

## Proje Yapısı Özeti

### Katmanlar

| Katman | Açıklama |
|--------|----------|
| **OmniFlow.Domain** | Entities, Enums, iş kuralı Exception'ları, Base sınıflar — hiçbir dış bağımlılığı yoktur |
| **OmniFlow.Application** | Features (CQRS), DTOs, Interfaces, Mappings, Wrappers, Application Exception'ları, KarmaService & NotificationService (orchestration) |
| **OmniFlow.Infrastructure** | DbContext (`IApplicationDbContext` implementasyonu), EF Core Configurations, Repository implementasyonları, dış API servisleri (AI, Email) |
| **OmniFlow.WebApi** | API Controllers (v1), Middlewares, API konfigürasyonu |
| **Tests** | Integration testler (endpoint/middleware), Infrastructure testler (Repository), Unit testler (Feature/Handler) |

### Exception Dağılımı

| Exception | Katman | Neden |
|-----------|--------|-------|
| `SelfFollowException` | Domain | Saf iş kuralı: "kendini takip edemezsin" |
| `SelfForkException` | Domain | Saf iş kuralı: "kendi rotanı fork'layamazsın" |
| `DuplicateUpvoteException` | Domain | Saf iş kuralı: "aynı içeriği iki kez beğenemezsin" |
| `ApiException` | Application | HTTP/API semantiği, handler'lar fırlatır |
| `ValidationException` | Application | Request validation hatası, FluentValidation pipeline'ı |
| `EntityNotFoundException` | Application | Repository sorgusu sonucu, Application concern'ü |
| `ForbiddenException` | Application | Yetkilendirme meselesi, Application/WebApi concern'ü |

### User vs ApplicationUser Stratejisi

| Model | Katman | Sorumluluk |
|-------|--------|-----------|
| `User.cs` | Domain/Entities | Saf domain modeli: karma, bio, followers, iş kuralları. Framework bağımlılığı yok |
| `ApplicationUser.cs` | Infrastructure/Models | ASP.NET Identity modeli: login, password hash, token. IdentityUser'dan türer |

İkisi aynı `users` tablosunu paylaşır. `ApplicationDbContext` içinde 1:1 mapping yapılır.

### Junction Tablo Erişim Stratejisi

`PostUpvote`, `CommentUpvote`, `TipUpvote`, `TripUpvote`, `SavedTrip` tablolarına ayrı repository yerine `IApplicationDbContext` üzerinden direkt erişim yapılır.

```csharp
// Ayrı repository yerine — doğrudan DbContext erişimi
await _context.Set<PostUpvote>().AddAsync(new PostUpvote { ... });
await _context.SaveChangesAsync();
```

### Entity — Tablo Eşleşmesi

| Entity | DB Tablosu | Katman | Repository |
|--------|-----------|--------|-----------|
| User | users | Core | `IUserRepositoryAsync` |
| Trip | trips | Core | `ITripRepositoryAsync` |
| Place | places | Core | `IPlaceRepositoryAsync` |
| Stop | stops | Core | `IStopRepositoryAsync` |
| Flight | flights | Core | `IFlightRepositoryAsync` |
| Hotel | hotels | Core | `IHotelRepositoryAsync` |
| Post | posts | Social | `IPostRepositoryAsync` |
| Comment | comments | Social | `ICommentRepositoryAsync` |
| CommunityTip | community_tips | Social | `ICommunityTipRepositoryAsync` |
| Follow | follows | Social | `IFollowRepositoryAsync` |
| PostUpvote | post_upvotes | Social | `IApplicationDbContext` |
| CommentUpvote | comment_upvotes | Social | `IApplicationDbContext` |
| TipUpvote | tip_upvotes | Social | `IApplicationDbContext` |
| TripUpvote | trip_upvotes | Social | `IApplicationDbContext` |
| SavedTrip | saved_trips | Social | `IApplicationDbContext` |
| Notification | notifications | System | `INotificationRepositoryAsync` |
| KarmaEvent | karma_events | System | `IKarmaEventRepositoryAsync` |
| RefreshToken | refresh_tokens | System | Infrastructure internal |

---

## MVP'den Çıkarılanlar (Bitirme Projesi)

Aşağıdaki bileşenler mevcut şemada **yoktur**, bitirme aşamasında eklenecektir:

| Bileşen | Açıklama |
|---------|---------|
| `UserBlock` entity + `UserBlockConfiguration` | Kullanıcı engelleme — MVP kapsamı dışı |
| `PushToken` entity + `PushTokenConfiguration` | Mobil push bildirimi — Flutter ile gelecek |
| `PushPlatform` enum | `PushToken` ile birlikte gelecek |
| `UserBlockRequest.cs` DTO | `UserBlock` ile birlikte gelecek |
| `BlockUser` / `UnblockUser` Commands | `UserBlock` ile birlikte gelecek |
| `VerifyEmailRequest.cs` DTO | Email doğrulama akışı — MVP'de basit tutuldu |
| `ETL Pipeline` servisleri | Amadeus API geçişi için |
| `RAG / pgvector` servisleri | Semantik mekan arama |
| AI Agent servisleri | LLM yardımlı fork düzenleme |
| Admin Panel Controllers | İçerik moderasyonu |
| Weather API entegrasyonu | `fallback_place_id` otomasyonu |
```
