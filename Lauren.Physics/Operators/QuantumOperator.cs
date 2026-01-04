using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Operators;

public abstract class QuantumOperator : IEquatable<QuantumOperator>
{
    /// <exception cref="ArgumentException">
    ///     Thrown when occupiedX and occupiedZ have different lengths.
    /// </exception>
    protected QuantumOperator(BitArray occupiedX, BitArray occupiedZ, Coefficient coefficient = Coefficient.PlusI)
    {
        if (occupiedX.Length != occupiedZ.Length)
            throw new ArgumentException("OccupiedX and OccupiedZ must have the same length.");

        OccupiedX = occupiedX;
        OccupiedZ = occupiedZ;
        Coefficient = coefficient;
    }

    /// <summary>
    ///     BitArray representing which qubits have X operations applied.
    /// </summary>
    public BitArray OccupiedX { get; }

    /// <summary>
    ///     BitArray representing which qubits have Z operations applied.
    /// </summary>
    public BitArray OccupiedZ { get; }

    /// <summary>
    ///     Coefficient of the quantum operator.
    /// </summary>
    public Coefficient Coefficient { get; }

    /// <summary>
    ///     The weight, i.e. the number of qubits this operator acts non-trivially on,
    ///     counting X and Z on the same qubit separately.
    /// </summary>
    public int Weight => OccupiedX.Weight + OccupiedZ.Weight;

    /// <summary>
    ///     The reduced weight, i.e. the number of qubits this operator acts non-trivially on,
    ///     counting X and Z on the same qubit only once.
    /// </summary>
    public int ReducedWeight => OccupiedX.Or(OccupiedZ).Weight;

    public bool Equals(QuantumOperator? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (OccupiedX.Length != other.OccupiedX.Length ||
            OccupiedZ.Length != other.OccupiedZ.Length)
            return false;

        for (var i = 0; i < OccupiedX.Length; i++)
            if (OccupiedX[i] != other.OccupiedX[i])
                return false;

        for (var i = 0; i < OccupiedZ.Length; i++)
            if (OccupiedZ[i] != other.OccupiedZ[i])
                return false;

        return Coefficient == other.Coefficient;
    }

    /// <summary>
    ///     Builds a BitArray with the X and Z occupations zipped together (X0, Z0, X1, Z1, ...).
    ///     For example, if OccupiedX = [true, false, true] and OccupiedZ = [false, true, false],
    ///     the result will be [true, false, false, true, true, false].
    /// </summary>
    public BitArray ZippedOccupations()
    {
        var result = new BitArray(OccupiedX.Length * 2);
        for (var i = 0; i < OccupiedX.Length; i++)
        {
            result[i * 2] = OccupiedX[i];
            result[i * 2 + 1] = OccupiedZ[i];
        }

        return result;
    }

    /// <summary>
    ///     Multiplies this quantum operator with another quantum operator at its right.
    ///     `this @ other` in mathematical notation.
    /// </summary>
    public abstract QuantumOperator Multiply(QuantumOperator other);

    /// <summary>
    ///     Multiplies this quantum operator with a coefficient.
    /// </summary>
    public abstract QuantumOperator Multiply(Coefficient coefficient);

    /// <summary>
    ///     The same as multiplying this quantum operator by -1.
    /// </summary>
    public abstract QuantumOperator Negate();

    /// <summary>
    ///     Returns the dual of this quantum operator.
    /// </summary>
    public abstract QuantumOperator Dual();

    /// <summary>
    ///     Returns true if this quantum operator is Hermitian.
    /// </summary>
    public abstract bool IsHermitian();

    /// <summary>
    ///     Clones this quantum operator.
    /// </summary>
    public abstract QuantumOperator Clone();

    public override bool Equals(object? obj)
    {
        return obj is QuantumOperator other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(OccupiedX, OccupiedZ, Coefficient);
    }
}