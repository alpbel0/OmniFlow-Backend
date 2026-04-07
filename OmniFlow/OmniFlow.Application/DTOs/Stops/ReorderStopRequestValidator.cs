using FluentValidation;

namespace OmniFlow.Application.DTOs.Stops;

public class ReorderStopRequestValidator : AbstractValidator<ReorderStopRequest>
{
    public ReorderStopRequestValidator()
    {
        RuleFor(x => x.StopId)
            .NotEmpty().WithMessage("StopId is required.");

        RuleFor(x => x.NewDayNumber)
            .GreaterThan(0).WithMessage("NewDayNumber must be greater than 0.");
    }
}