using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Currency;
using OmniFlow.Application.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Users.Commands.UpdateCurrencyPreference;

public sealed record UpdateCurrencyPreferenceCommand(string CurrencyCode) : IRequest<Unit>;

public sealed class UpdateCurrencyPreferenceCommandValidator : AbstractValidator<UpdateCurrencyPreferenceCommand>
{
    public UpdateCurrencyPreferenceCommandValidator()
    {
        RuleFor(x => x.CurrencyCode)
            .Must(CurrencyPolicy.IsSupported)
            .WithMessage("Currency must be TRY, USD, or EUR.");
    }
}

public sealed class UpdateCurrencyPreferenceCommandHandler(
    IApplicationDbContext context,
    IAuthenticatedUserService authenticatedUserService)
    : IRequestHandler<UpdateCurrencyPreferenceCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCurrencyPreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(authenticatedUserService.UserId);
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new EntityNotFoundException("User", userId);
        user.PreferredCurrencyCode = CurrencyPolicy.Normalize(request.CurrencyCode);
        await context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
