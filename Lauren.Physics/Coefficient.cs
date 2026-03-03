using System.Numerics;

namespace Lauren.Physics;

/// <summary>
///     Coefficient for quantum operations.
///     Only contains four values: +1, -1, +1j, -1j.
/// </summary>
public enum Coefficient
{
    PlusOne,
    MinusOne,
    PlusI,
    MinusI
}

public static class CoefficientExtensions
{
    private static int ToIndex(Coefficient value)
    {
        int index = (int)value;
        if ((uint)index >= 4u)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Invalid coefficient value.");
        }

        return index;
    }

    private static readonly Complex[] ComplexByCoefficient =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];

    // Index: (base << 2) | exponentMod4
    private static readonly Coefficient[] PowerTable =
    [
        Coefficient.PlusOne, Coefficient.PlusOne, Coefficient.PlusOne, Coefficient.PlusOne,
        Coefficient.PlusOne, Coefficient.MinusOne, Coefficient.PlusOne, Coefficient.MinusOne,
        Coefficient.PlusOne, Coefficient.PlusI, Coefficient.MinusOne, Coefficient.MinusI,
        Coefficient.PlusOne, Coefficient.MinusI, Coefficient.MinusOne, Coefficient.PlusI
    ];

    // Index: (left << 2) | right
    private static readonly Coefficient[] MultiplyTable =
    [
        Coefficient.PlusOne, Coefficient.MinusOne, Coefficient.PlusI, Coefficient.MinusI,
        Coefficient.MinusOne, Coefficient.PlusOne, Coefficient.MinusI, Coefficient.PlusI,
        Coefficient.PlusI, Coefficient.MinusI, Coefficient.MinusOne, Coefficient.PlusOne,
        Coefficient.MinusI, Coefficient.PlusI, Coefficient.PlusOne, Coefficient.MinusOne
    ];

    extension(Coefficient coefficient)
    {
        /// <summary>
        ///     Get the complex value of the coefficient.
        /// </summary>
        public Complex ToComplex() => ComplexByCoefficient[ToIndex(coefficient)];
        
        /// <summary>
        ///     Return the Coefficient corresponding to the given complex number.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown when the complex number is not one of: 1, -1, i, -i.
        /// </exception>
        public static Coefficient FromComplex(Complex complex)
        {
            return complex switch
            {
                { Real: 1, Imaginary: 0 } => Coefficient.PlusOne,
                { Real: -1, Imaginary: 0 } => Coefficient.MinusOne,
                { Real: 0, Imaginary: 1 } => Coefficient.PlusI,
                { Real: 0, Imaginary: -1 } => Coefficient.MinusI,
                _ => throw new ArgumentException("Complex number must be one of: 1, -1, i, -i.")
            };
        }

        /// <summary>
        ///     Get the coefficient raised to the given exponent.
        ///     For example, Coefficient.PlusI.Power(2) = Coefficient.MinusOne.
        /// </summary>
        public Coefficient Power(int exponent)
        {
            int normalizedExponent = exponent & 0b11;
            return PowerTable[(ToIndex(coefficient) << 2) | normalizedExponent];
        }

        /// <summary>
        ///     Multiply two coefficients together.
        /// </summary>
        public static Coefficient operator *(Coefficient a, Coefficient b) =>
            MultiplyTable[(ToIndex(a) << 2) | ToIndex(b)];

        /// <summary>
        ///     Check if the coefficient is real (i.e., +1 or -1).
        /// </summary>
        public bool IsReal() => ToIndex(coefficient) <= 1;

        /// <summary>
        ///     Check if the coefficient is imaginary (i.e., +i or -i).
        /// </summary>
        public bool IsImaginary() => ToIndex(coefficient) >= 2;
    }
}
