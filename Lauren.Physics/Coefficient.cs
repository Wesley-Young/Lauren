using System.Numerics;

namespace Lauren.Physics;

/// <summary>
///     Coefficient for quantum operations.
///     Only contains four values: +1, -1, +1j, -1j.
/// </summary>
public enum Coefficient
{
    PlusOne = 0,
    PlusI = 1,
    MinusOne = 2,
    MinusI = 3
}

public static class CoefficientExtensions
{
    private static readonly Complex[] ComplexByCoefficient =
    [
        new(1, 0),
        new(0, 1),
        new(-1, 0),
        new(0, -1)
    ];

    extension(Coefficient coefficient)
    {
        public int ToPhase() => (int)coefficient;

        /// <summary>
        ///     Get the complex value of the coefficient.
        /// </summary>
        public Complex ToComplex() => ComplexByCoefficient[coefficient.ToPhase()];

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
            return (Coefficient)((coefficient.ToPhase() * normalizedExponent) & 0b11);
        }

        /// <summary>
        ///     Multiply two coefficients together.
        /// </summary>
        public static Coefficient operator *(Coefficient a, Coefficient b) =>
            (Coefficient)((a.ToPhase() + b.ToPhase()) & 0b11);

        /// <summary>
        ///     Check if the coefficient is real (i.e., +1 or -1).
        /// </summary>
        public bool IsReal() => (coefficient.ToPhase() & 1) == 0;

        /// <summary>
        ///     Check if the coefficient is imaginary (i.e., +i or -i).
        /// </summary>
        public bool IsImaginary() => (coefficient.ToPhase() & 1) != 0;
    }
}