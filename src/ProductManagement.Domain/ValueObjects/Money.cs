using ProductManagement.Domain.Common;

namespace ProductManagement.Domain.ValueObjects;

/// <summary>
/// Monetary amount with an ISO-4217 currency. Immutable value object; mapped as
/// an EF Core owned type (two columns) so prices never lose their currency.
/// </summary>
public sealed record Money
{
    // Parameterless ctor for EF Core materialisation.
    private Money() => Currency = string.Empty;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private init; }
    public string Currency { get; private init; }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Error.Validation("Money.NegativeAmount", "Price amount cannot be negative.");

        if (amount > 9_999_999_999.99m)
            return Error.Validation("Money.AmountTooLarge", "Price amount exceeds the supported maximum.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            return Error.Validation("Money.InvalidCurrency", "Currency must be a 3-letter ISO-4217 code.");

        // Store at 2-dp scale; banker's rounding avoids systematic bias.
        var rounded = Math.Round(amount, 2, MidpointRounding.ToEven);
        return new Money(rounded, currency.Trim().ToUpperInvariant());
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
