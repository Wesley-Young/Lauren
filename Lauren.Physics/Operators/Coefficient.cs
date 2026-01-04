using System.Numerics;

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

namespace Lauren.Physics.Operators;

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
    extension(Coefficient coefficient)
    {
        /// <summary>
        ///     Get the complex value of the coefficient.
        /// </summary>
        public Complex ToComplex()
        {
            return coefficient switch
            {
                Coefficient.PlusOne => new Complex(1, 0),
                Coefficient.MinusOne => new Complex(-1, 0),
                Coefficient.PlusI => new Complex(0, 1),
                Coefficient.MinusI => new Complex(0, -1)
            };
        }
        
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
            var exp = exponent % 4;
            return (coefficient, exp) switch
            {
                (_, 0) => Coefficient.PlusOne,
                (Coefficient.PlusOne, 1) => Coefficient.PlusOne,
                (Coefficient.MinusOne, 1) => Coefficient.MinusOne,
                (Coefficient.PlusI, 1) => Coefficient.PlusI,
                (Coefficient.MinusI, 1) => Coefficient.MinusI,
                (Coefficient.PlusOne, 2) => Coefficient.PlusOne,
                (Coefficient.MinusOne, 2) => Coefficient.PlusOne,
                (Coefficient.PlusI, 2) => Coefficient.MinusOne,
                (Coefficient.MinusI, 2) => Coefficient.MinusOne,
                (Coefficient.PlusOne, 3) => Coefficient.PlusOne,
                (Coefficient.MinusOne, 3) => Coefficient.MinusOne,
                (Coefficient.PlusI, 3) => Coefficient.MinusI,
                (Coefficient.MinusI, 3) => Coefficient.PlusI
            };
        }

        /// <summary>
        ///     Multiply two coefficients together.
        /// </summary>
        public static Coefficient operator *(Coefficient a, Coefficient b)
        {
            return (a, b) switch
            {
                (Coefficient.PlusOne, _) => b,
                (Coefficient.MinusOne, Coefficient.PlusOne) => Coefficient.MinusOne,
                (Coefficient.MinusOne, Coefficient.MinusOne) => Coefficient.PlusOne,
                (Coefficient.MinusOne, Coefficient.PlusI) => Coefficient.MinusI,
                (Coefficient.MinusOne, Coefficient.MinusI) => Coefficient.PlusI,
                (Coefficient.PlusI, Coefficient.PlusOne) => Coefficient.PlusI,
                (Coefficient.PlusI, Coefficient.MinusOne) => Coefficient.MinusI,
                (Coefficient.PlusI, Coefficient.PlusI) => Coefficient.MinusOne,
                (Coefficient.PlusI, Coefficient.MinusI) => Coefficient.PlusOne,
                (Coefficient.MinusI, Coefficient.PlusOne) => Coefficient.MinusI,
                (Coefficient.MinusI, Coefficient.MinusOne) => Coefficient.PlusI,
                (Coefficient.MinusI, Coefficient.PlusI) => Coefficient.PlusOne,
                (Coefficient.MinusI, Coefficient.MinusI) => Coefficient.MinusOne
            };
        }
    }
}