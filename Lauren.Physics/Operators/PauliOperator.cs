using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Operators;

public class PauliOperator : QuantumOperator
{
    public PauliOperator(BitArray occupiedX, BitArray occupiedZ, Coefficient coefficient = Coefficient.PlusI)
        : base(occupiedX, occupiedZ, coefficient)
    {
    }

    internal PauliOperator(PackedBits occupiedX, PackedBits occupiedZ, Coefficient coefficient = Coefficient.PlusI)
        : base(occupiedX, occupiedZ, coefficient)
    {
    }

    public override PauliOperator Multiply(QuantumOperator other)
    {
        if (other is not PauliOperator pauli)
        {
            throw new ArgumentException("Can only multiply PauliOperator by another PauliOperator.", nameof(other));
        }

        var newOccupiedX = OccupiedXPacked.Clone();
        newOccupiedX.XorInPlace(pauli.OccupiedXPacked);

        var newOccupiedZ = OccupiedZPacked.Clone();
        newOccupiedZ.XorInPlace(pauli.OccupiedZPacked);

        var newCoefficient = Coefficient * pauli.Coefficient;
        return new PauliOperator(newOccupiedX, newOccupiedZ, newCoefficient);
    }

    public override PauliOperator Multiply(Coefficient coefficient) =>
        new(OccupiedXPacked.Clone(), OccupiedZPacked.Clone(), Coefficient * coefficient);

    public override PauliOperator Negate() =>
        new(OccupiedXPacked.Clone(), OccupiedZPacked.Clone(), Coefficient * Coefficient.MinusOne);

    public override PauliOperator Dual() =>
        new(OccupiedZPacked.Clone(), OccupiedXPacked.Clone(), Coefficient);

    public override bool IsHermitian()
    {
        int andWeight = PackedBits.AndWeight(OccupiedXPacked, OccupiedZPacked);
        return andWeight % 2 == 0 ? Coefficient.IsReal() : Coefficient.IsImaginary();
    }

    public override bool CommutesWith(QuantumOperator other)
    {
        if (other is not PauliOperator pauli)
        {
            throw new ArgumentException("Can only check commutation with another PauliOperator.", nameof(other));
        }

        int overlapXWithZ = PackedBits.AndWeight(OccupiedXPacked, pauli.OccupiedZPacked);
        int overlapZWithX = PackedBits.AndWeight(OccupiedZPacked, pauli.OccupiedXPacked);
        return ((overlapXWithZ + overlapZWithX) & 1) == 0;
    }

    public override PauliOperator Clone() =>
        new(OccupiedXPacked.Clone(), OccupiedZPacked.Clone(), Coefficient);

    /// <summary>
    ///     Create a Hermitian Pauli operator with correct coefficient based on occupied X and Z bits.
    /// </summary>
    public static PauliOperator CreateHermitian(BitArray occupiedX, BitArray occupiedZ)
    {
        var packedX = new PackedBits(occupiedX);
        var packedZ = new PackedBits(occupiedZ);
        int andWeight = PackedBits.AndWeight(packedX, packedZ);
        var coefficient = andWeight % 2 == 0 ? Coefficient.PlusOne : Coefficient.PlusI;
        return new PauliOperator(packedX, packedZ, coefficient);
    }
}