using System.Numerics;
using Lauren.Physics.Operators;
using Xunit;

namespace Lauren.Physics.Tests;

public class CoefficientExtensionsTests
{
    public static TheoryData<Coefficient, Complex> ToComplexData => new()
    {
        { Coefficient.PlusOne, new Complex(1, 0) },
        { Coefficient.MinusOne, new Complex(-1, 0) },
        { Coefficient.PlusI, new Complex(0, 1) },
        { Coefficient.MinusI, new Complex(0, -1) }
    };

    public static TheoryData<Complex, Coefficient> FromComplexData => new()
    {
        { new Complex(1, 0), Coefficient.PlusOne },
        { new Complex(-1, 0), Coefficient.MinusOne },
        { new Complex(0, 1), Coefficient.PlusI },
        { new Complex(0, -1), Coefficient.MinusI }
    };

    public static TheoryData<Complex> InvalidComplexData =>
    [
        new Complex(2, 0),
        new Complex(0.5, 0),
        new Complex(1, 1),
        new Complex(0, 2)
    ];

    public static TheoryData<Coefficient, int, Coefficient> PowerData => new()
    {
        { Coefficient.PlusOne, 0, Coefficient.PlusOne },
        { Coefficient.MinusOne, 0, Coefficient.PlusOne },
        { Coefficient.PlusI, 1, Coefficient.PlusI },
        { Coefficient.MinusI, 1, Coefficient.MinusI },
        { Coefficient.PlusI, 2, Coefficient.MinusOne },
        { Coefficient.MinusI, 2, Coefficient.MinusOne },
        { Coefficient.PlusI, 3, Coefficient.MinusI },
        { Coefficient.MinusI, 3, Coefficient.PlusI },
        { Coefficient.MinusOne, 3, Coefficient.MinusOne },
        { Coefficient.MinusOne, 5, Coefficient.MinusOne }
    };

    public static TheoryData<Coefficient, Coefficient, Coefficient> MultiplyData => new()
    {
        { Coefficient.PlusOne, Coefficient.PlusOne, Coefficient.PlusOne },
        { Coefficient.PlusOne, Coefficient.MinusOne, Coefficient.MinusOne },
        { Coefficient.PlusOne, Coefficient.PlusI, Coefficient.PlusI },
        { Coefficient.PlusOne, Coefficient.MinusI, Coefficient.MinusI },
        { Coefficient.MinusOne, Coefficient.PlusOne, Coefficient.MinusOne },
        { Coefficient.MinusOne, Coefficient.MinusOne, Coefficient.PlusOne },
        { Coefficient.MinusOne, Coefficient.PlusI, Coefficient.MinusI },
        { Coefficient.MinusOne, Coefficient.MinusI, Coefficient.PlusI },
        { Coefficient.PlusI, Coefficient.PlusOne, Coefficient.PlusI },
        { Coefficient.PlusI, Coefficient.MinusOne, Coefficient.MinusI },
        { Coefficient.PlusI, Coefficient.PlusI, Coefficient.MinusOne },
        { Coefficient.PlusI, Coefficient.MinusI, Coefficient.PlusOne },
        { Coefficient.MinusI, Coefficient.PlusOne, Coefficient.MinusI },
        { Coefficient.MinusI, Coefficient.MinusOne, Coefficient.PlusI },
        { Coefficient.MinusI, Coefficient.PlusI, Coefficient.PlusOne },
        { Coefficient.MinusI, Coefficient.MinusI, Coefficient.MinusOne }
    };

    [Theory]
    [MemberData(nameof(ToComplexData))]
    public void ToComplex_ReturnsExpected(Coefficient coefficient, Complex expected)
    {
        var actual = coefficient.ToComplex();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(FromComplexData))]
    public void FromComplex_ReturnsExpected(Complex value, Coefficient expected)
    {
        var actual = Coefficient.FromComplex(value);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(InvalidComplexData))]
    public void FromComplex_InvalidValue_Throws(Complex value)
    {
        Assert.Throws<ArgumentException>(() => Coefficient.FromComplex(value));
    }

    [Theory]
    [MemberData(nameof(PowerData))]
    public void Power_ReturnsExpected(Coefficient coefficient, int exponent, Coefficient expected)
    {
        var actual = coefficient.Power(exponent);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(MultiplyData))]
    public void Multiply_ReturnsExpected(Coefficient left, Coefficient right, Coefficient expected)
    {
        var actual = left * right;

        Assert.Equal(expected, actual);
    }
}
