using OmniFlow.Application.Features.Trips.Commands.CreateTrip;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Trips;

public class CreateTripCommandValidatorTests
{
    private readonly CreateTripCommandValidator _validator;

    public CreateTripCommandValidatorTests()
    {
        _validator = new CreateTripCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            BudgetTier = BudgetTier.Standard,
            TravelStyles = new List<TravelStyle> { TravelStyle.Adventure }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyTitle_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_TitleExceeds100Characters_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = new string('a', 101),
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_EmptyCity_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Origin");
    }

    [Fact]
    public void Validate_EmptyCountry_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OriginCountry");
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 7),
            EndDate = new DateOnly(2025, 6, 1),
            PersonCount = 2
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EndDate");
    }

    [Fact]
    public void Validate_PersonCountZero_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PersonCount");
    }

    [Fact]
    public void Validate_NegativeUserBudget_FailsValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            ManualBudget = -100
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ManualBudget");
    }

    [Fact]
    public void Validate_NullUserBudget_PassesValidation()
    {
        // Arrange
        var command = new CreateTripCommand
        {
            Title = "Test Trip",
            Origin = "Antalya",
            OriginCountry = "Turkey",
            StartDate = new DateOnly(2025, 6, 1),
            EndDate = new DateOnly(2025, 6, 7),
            PersonCount = 2,
            ManualBudget = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}