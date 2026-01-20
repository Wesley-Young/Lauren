using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Operators;

public class PauliOperator(BitArray occupiedX, BitArray occupiedZ, Coefficient coefficient = Coefficient.PlusI) :
    QuantumOperator(occupiedX, occupiedZ, coefficient)
{
    public override QuantumOperator Multiply(QuantumOperator other)
    {
        var newOccupiedX = ((BitArray)OccupiedX.Clone()).Xor(other.OccupiedX);
        var newOccupiedZ = ((BitArray)OccupiedZ.Clone()).Xor(other.OccupiedZ);
        var newCoefficient = Coefficient * other.Coefficient;
        return new PauliOperator(newOccupiedX, newOccupiedZ, newCoefficient);
    }

    public override QuantumOperator Multiply(Coefficient coefficient) =>
        new PauliOperator((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient * coefficient);

    public override QuantumOperator Negate() =>
        new PauliOperator((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient * Coefficient.MinusOne);

    public override QuantumOperator Dual() =>
        new PauliOperator((BitArray)OccupiedZ.Clone(), (BitArray)OccupiedX.Clone(), Coefficient);

    public override bool IsHermitian()
    {
        int andWeight = BitArray.AndWeight(OccupiedX, OccupiedZ);
        // X @ Z = iY, which breaks hermiticity; however i * i = -1, so even weights preserve hermiticity
        return andWeight % 2 == 0 ? Coefficient.IsReal() : Coefficient.IsImaginary();
    }

    public override QuantumOperator Clone() => new PauliOperator((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient);
}