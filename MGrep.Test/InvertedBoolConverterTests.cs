using System.Globalization;
using Shouldly;

namespace MGrep.Test;

public class InvertedBoolConverterTests
{
    [Fact]
    public void GivenFalseBooleanWhenConvertingThenReturnsTrueBoolean()
    {
        var converter = new InvertedBoolConverter();

        var result = converter.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(true);
    }

    [Fact]
    public void GivenTrueBooleanWhenConvertingThenReturnsFalseBoolean()
    {
        var converter = new InvertedBoolConverter();

        var result = converter.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void GivenStringWhenConvertingThenThrowsException()
    {
        var converter = new InvertedBoolConverter();

        Should.Throw<InvalidCastException>(() =>
            converter.Convert("Hello", typeof(bool), null, CultureInfo.InvariantCulture));
    }
}