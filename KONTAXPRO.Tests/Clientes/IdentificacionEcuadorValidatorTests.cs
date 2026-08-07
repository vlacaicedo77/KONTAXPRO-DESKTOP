using KONTAXPRO.Application.Clientes;

namespace KONTAXPRO.Tests.Clientes;

public sealed class IdentificacionEcuadorValidatorTests
{
    [Theory]
    [InlineData("CEDULA", "1710034065")]
    [InlineData("CEDULA", "3040091260")]
    [InlineData("CEDULA", "0962636643")]
    [InlineData("RUC", "1710034065001")]
    [InlineData("RUC", "1790016919001")]
    [InlineData("RUC", "1760001550001")]
    [InlineData("RUC", "1790016918001")]
    [InlineData("PASAPORTE", "AB-12345")]
    public void Validate_AcceptsValidIdentification(string type, string number)
    {
        var result = IdentificacionEcuadorValidator.Validate(type, number);

        Assert.True(result.IsValid, result.Error);
    }

    [Theory]
    [InlineData("CEDULA", "1710034064")]
    [InlineData("CEDULA", "2300257941")]
    [InlineData("CEDULA", "171003406")]
    [InlineData("CEDULA", "171003406A")]
    [InlineData("RUC", "179001691800")]
    [InlineData("RUC", "179001691800A")]
    [InlineData("PASAPORTE", "A@12")]
    public void Validate_RejectsInvalidIdentification(string type, string number)
    {
        var result = IdentificacionEcuadorValidator.Validate(type, number);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Theory]
    [InlineData("1710034065", "CEDULA")]
    [InlineData("1710034065001", "RUC")]
    [InlineData("171003406", null)]
    [InlineData("AB12345678", null)]
    public void DetectNationalType_UsesStandardLength(
        string number,
        string? expectedType)
    {
        Assert.Equal(
            expectedType,
            IdentificacionEcuadorValidator.DetectNationalType(number));
    }

    [Fact]
    public void NaturalPersonCedulaAndRucAreEquivalent()
    {
        Assert.True(IdentificacionEcuadorValidator.BelongToSameNaturalPerson(
            "CEDULA",
            "1710034065",
            "RUC",
            "1710034065001"));
    }

    [Fact]
    public void CompanyRucIsNotEquivalentToCedula()
    {
        Assert.False(IdentificacionEcuadorValidator.BelongToSameNaturalPerson(
            "CEDULA",
            "1790016919",
            "RUC",
            "1790016919001"));
    }

    [Fact]
    public void InvalidCedulaBaseIsNotEquivalentToRuc()
    {
        Assert.False(IdentificacionEcuadorValidator.BelongToSameNaturalPerson(
            "CEDULA",
            "2300257941",
            "RUC",
            "2300257941001"));
    }
}
