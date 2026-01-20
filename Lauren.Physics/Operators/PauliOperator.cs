using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Operators;

public class PauliOperator(BitArray occupiedX, BitArray occupiedZ, Coefficient coefficient = Coefficient.PlusI) :
    QuantumOperator(occupiedX, occupiedZ, coefficient)
{
    public override PauliOperator Multiply(QuantumOperator other)
    {
        if (other is not PauliOperator)
            throw new ArgumentException("Can only multiply PauliOperator by another PauliOperator.", nameof(other));
        var newOccupiedX = ((BitArray)OccupiedX.Clone()).Xor(other.OccupiedX);
        var newOccupiedZ = ((BitArray)OccupiedZ.Clone()).Xor(other.OccupiedZ);
        var newCoefficient = Coefficient * other.Coefficient;
        return new PauliOperator(newOccupiedX, newOccupiedZ, newCoefficient);
    }

    public override PauliOperator Multiply(Coefficient coefficient) =>
        new((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient * coefficient);

    public override PauliOperator Negate() =>
        new((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient * Coefficient.MinusOne);

    public override PauliOperator Dual() =>
        new((BitArray)OccupiedZ.Clone(), (BitArray)OccupiedX.Clone(), Coefficient);

    public override bool IsHermitian()
    {
        int andWeight = BitArray.AndWeight(OccupiedX, OccupiedZ);
        // X @ Z = iY, which breaks hermiticity; however i * i = -1, so even weights preserve hermiticity
        return andWeight % 2 == 0 ? Coefficient.IsReal() : Coefficient.IsImaginary();
    }

    public override PauliOperator Clone() => new ((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient);

    public static PauliOperator CreateHermitian(BitArray occupiedX, BitArray occupiedZ)
    {
        int andWeight = BitArray.AndWeight(occupiedX, occupiedZ);
        var coefficient = andWeight % 2 == 0 ? Coefficient.PlusOne : Coefficient.PlusI;
        return new PauliOperator(occupiedX, occupiedZ, coefficient);
    }
}