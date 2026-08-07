using KONTAXPRO.Application.Clientes;

namespace KONTAXPRO.Tests.Clientes;

public sealed class ContactoClienteNormalizerTests
{
    [Theory]
    [InlineData("0999999999", "+593999999999")]
    [InlineData("593999999999", "+593999999999")]
    [InlineData("+593 99 999 9999", "+593999999999")]
    [InlineData("022345678", "+59322345678")]
    [InlineData("+1 (415) 555-2671", "+14155552671")]
    [InlineData("00939999999999", "+939999999999")]
    public void NormalizePhone_ReturnsE164(string input, string expected)
    {
        var result = ContactoClienteNormalizer.NormalizePhone(input);

        Assert.True(result.IsValid, result.Error);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("099")]
    [InlineData("telefono0999999999")]
    [InlineData("593")]
    [InlineData("12+34567890")]
    public void NormalizePhone_RejectsInvalidValue(string input)
    {
        var result = ContactoClienteNormalizer.NormalizePhone(input);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void NormalizeEmail_UsesLowercase()
    {
        var result = ContactoClienteNormalizer.NormalizeEmail(
            "  CLIENTE@Example.COM ");

        Assert.True(result.IsValid);
        Assert.Equal("cliente@example.com", result.Value);
    }

    [Fact]
    public void NormalizeEmail_WithSeveralAddressesUsesFirstOne()
    {
        var result = ContactoClienteNormalizer.NormalizeEmail(
            "contabilidad@pollosdelsur.com,gerencia@pollosdelsur.com");

        Assert.True(result.IsValid, result.Error);
        Assert.Equal("contabilidad@pollosdelsur.com", result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyEmailUsesDefaultAndEmptyPhoneRemainsOptional(string? input)
    {
        var email = ContactoClienteNormalizer.NormalizeEmail(input);

        Assert.True(email.IsValid);
        Assert.Equal(ContactoClienteNormalizer.DefaultEmail, email.Value);
        Assert.True(ContactoClienteNormalizer.NormalizePhone(input).IsValid);
    }
}
