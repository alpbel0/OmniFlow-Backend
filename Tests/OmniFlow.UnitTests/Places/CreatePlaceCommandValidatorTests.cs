using OmniFlow.Application.Features.Places.Commands.CreatePlace;
using OmniFlow.Domain.Enums;

namespace OmniFlow.UnitTests.Places;

public class CreatePlaceCommandValidatorTests
{
    private readonly CreatePlaceCommandValidator _validator;

    public CreatePlaceCommandValidatorTests()
    {
        _validator = new CreatePlaceCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test Place",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            EstimatedPrice = 100,
            CurrencyCode = "USD"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_NameExceeds255Characters_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = new string('a', 256),
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_EmptyCity_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City");
    }

    [Fact]
    public void Validate_EmptyCountry_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "",
            Latitude = 36.8,
            Longitude = 30.7
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void Validate_LatitudeOutOfRange_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 91.0,
            Longitude = 30.7
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Latitude");
    }

    [Fact]
    public void Validate_LongitudeOutOfRange_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 181.0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Longitude");
    }

    [Fact]
    public void Validate_RatingBelowRange_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            Rating = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rating");
    }

    [Fact]
    public void Validate_RatingAboveRange_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            Rating = 6
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Rating");
    }

    [Fact]
    public void Validate_NullRating_PassesValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            Rating = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeEstimatedPrice_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            EstimatedPrice = -10
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "EstimatedPrice");
    }

    [Fact]
    public void Validate_InvalidCurrencyCode_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            CurrencyCode = "US"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Fact]
    public void Validate_LowercaseCurrencyCode_FailsValidation()
    {
        // Arrange
        var command = new CreatePlaceCommand
        {
            Name = "Test",
            Category = PlaceCategory.Restaurant,
            City = "Antalya",
            Country = "Turkey",
            Latitude = 36.8,
            Longitude = 30.7,
            CurrencyCode = "usd"
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }
}