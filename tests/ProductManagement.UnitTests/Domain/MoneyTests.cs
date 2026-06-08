using FluentAssertions;
using ProductManagement.Domain.ValueObjects;

namespace ProductManagement.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndCurrency_Succeeds()
    {
        var result = Money.Create(19.999m, "usd");

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(20.00m);          // rounded to 2dp
        result.Value.Currency.Should().Be("USD");          // normalised upper-case
    }

    [Fact]
    public void Create_WithNegativeAmount_Fails()
    {
        var result = Money.Create(-1m, "USD");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    [InlineData("")]
    public void Create_WithInvalidCurrency_Fails(string currency)
    {
        var result = Money.Create(10m, currency);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrency");
    }
}
