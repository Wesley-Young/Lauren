using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Operators;

public abstract class QuantumOperator : IEquatable<QuantumOperator>
{
    private readonly PackedBits _occupiedX;
    private readonly PackedBits _occupiedZ;
    private PackedBits? _zippedOccupationsPacked;

    /// <exception cref="ArgumentException">
    ///     Thrown when occupiedX and occupiedZ have different lengths.
    /// </exception>
    protected QuantumOperator(BitArray occupiedX, BitArray occupiedZ, Coefficient coefficient = Coefficient.PlusI)
        : this(new PackedBits(occupiedX), new PackedBits(occupiedZ), coefficient)
    {
    }

    /// <exception cref="ArgumentException">
    ///     Thrown when occupiedX and occupiedZ have different lengths.
    /// </exception>
    internal QuantumOperator(PackedBits occupiedX, PackedBits occupiedZ, Coefficient coefficient = Coefficient.PlusI)
    {
        if (occupiedX.Length != occupiedZ.Length)
        {
            throw new ArgumentException("OccupiedX and OccupiedZ must have the same length.");
        }

        _occupiedX = occupiedX;
        _occupiedZ = occupiedZ;
        Coefficient = coefficient;
    }

    /// <summary>
    ///     BitArray representing which qubits have X operations applied.
    /// </summary>
    public BitArray OccupiedX => _occupiedX.ToBitArray();

    /// <summary>
    ///     BitArray representing which qubits have Z operations applied.
    /// </summary>
    public BitArray OccupiedZ => _occupiedZ.ToBitArray();

    /// <summary>
    ///     Coefficient of the quantum operator.
    /// </summary>
    public Coefficient Coefficient { get; }

    internal PackedBits OccupiedXPacked => _occupiedX;

    internal PackedBits OccupiedZPacked => _occupiedZ;

    /// <summary>
    ///     The weight, i.e. the number of qubits this operator acts non-trivially on,
    ///     counting X and Z on the same qubit separately.
    /// </summary>
    public int Weight => _occupiedX.Weight() + _occupiedZ.Weight();

    /// <summary>
    ///     The reduced weight, i.e. the number of qubits this operator acts non-trivially on,
    ///     counting X and Z on the same qubit only once.
    /// </summary>
    public int ReducedWeight => PackedBits.OrWeight(_occupiedX, _occupiedZ);

    public bool Equals(QuantumOperator? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!_occupiedX.Equals(other._occupiedX))
        {
            return false;
        }

        if (!_occupiedZ.Equals(other._occupiedZ))
        {
            return false;
        }

        return Coefficient == other.Coefficient;
    }

    /// <summary>
    ///     Builds a BitArray with the X and Z occupations zipped together (X0, Z0, X1, Z1, ...).
    ///     For example, if OccupiedX = [true, false, true] and OccupiedZ = [false, true, false],
    ///     the result will be [true, false, false, true, true, false].
    /// </summary>
    public BitArray ZippedOccupations() => ZippedOccupationsPacked().ToBitArray();

    internal PackedBits ZippedOccupationsPacked() =>
        _zippedOccupationsPacked ??= PackedBits.ZipPauli(_occupiedX, _occupiedZ);

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
    ///     Returns true if this quantum operator commutes with another quantum operator,
    ///     i.e. `this @ other == other @ this`.
    /// </summary>
    public abstract bool CommutesWith(QuantumOperator other);

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
        return HashCode.Combine(_occupiedX, _occupiedZ, Coefficient);
    }
}