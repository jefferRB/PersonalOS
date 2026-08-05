using PersonalOS.Domain.Users;

namespace PersonalOS.UnitTests.Users;

public sealed class DisplayNameRulesTests
{
    [Theory]
    [InlineData("Jefferson", "Jefferson")]
    [InlineData("  Jefferson  ", "Jefferson")]
    [InlineData("\tJefferson Rojas\n", "Jefferson Rojas")]
    [InlineData("Jo", "Jo")]
    public void TryNormalize_WithAcceptableValue_TrimsAndAccepts(string value, string expected)
    {
        var normalized = DisplayNameRules.TryNormalize(value, out var result);

        Assert.True(normalized);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("      ")]
    [InlineData("\t\n")]
    [InlineData("J")]
    [InlineData("  J  ")]
    public void TryNormalize_WithWhitespaceOnlyOrTooShortValue_IsRejected(string? value)
    {
        var normalized = DisplayNameRules.TryNormalize(value, out var result);

        Assert.False(normalized);
        Assert.Null(result);
    }

    [Fact]
    public void TryNormalize_AtMaximumLength_IsAccepted()
    {
        var value = new string('a', DisplayNameRules.MaxLength);

        Assert.True(DisplayNameRules.TryNormalize(value, out var result));
        Assert.Equal(value, result);
    }

    [Fact]
    public void TryNormalize_AboveMaximumLength_IsRejected()
    {
        var value = new string('a', DisplayNameRules.MaxLength + 1);

        Assert.False(DisplayNameRules.TryNormalize(value, out _));
    }

    [Fact]
    public void TryNormalize_CountsLengthAfterTrimming()
    {
        var value = "   " + new string('a', DisplayNameRules.MaxLength) + "   ";

        Assert.True(DisplayNameRules.TryNormalize(value, out var result));
        Assert.Equal(DisplayNameRules.MaxLength, result!.Length);
    }
}
