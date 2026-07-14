using MediatR;
using FluentValidation;
using OmniFlow.Application.Currency;
using OmniFlow.Application.DTOs.Currency;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Features.Currency.Queries.GetCurrencyRates;

public sealed record GetCurrencyRatesQuery(string BaseCurrencyCode, DateOnly? Date)
    : IRequest<CurrencyRatesResponse>;

public sealed class GetCurrencyRatesQueryValidator : AbstractValidator<GetCurrencyRatesQuery>
{
    public GetCurrencyRatesQueryValidator()
    {
        RuleFor(x => x.BaseCurrencyCode)
            .NotEmpty()
            .Must(CurrencyPolicy.IsSupported)
            .WithMessage("Base currency must be TRY, USD, or EUR.");
    }
}

public sealed class GetCurrencyRatesQueryHandler(IExchangeRateService exchangeRateService)
    : IRequestHandler<GetCurrencyRatesQuery, CurrencyRatesResponse>
{
    public async Task<CurrencyRatesResponse> Handle(GetCurrencyRatesQuery request, CancellationToken cancellationToken)
    {
        var baseCurrency = CurrencyPolicy.Normalize(request.BaseCurrencyCode);
        var rates = new List<CurrencyRateItem>();
        foreach (var quote in CurrencyPolicy.Supported.OrderBy(x => x))
        {
            var result = await exchangeRateService.GetRateAsync(baseCurrency, quote, request.Date, cancellationToken);
            rates.Add(new CurrencyRateItem(
                quote,
                result.Rate,
                result.RequestedDate,
                result.EffectiveDate,
                result.Provider));
        }

        return new CurrencyRatesResponse(baseCurrency, rates);
    }
}
