using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Operators;

/// <summary>
///     Represents a Majorana operator in the stabilizer formalism.
/// </summary>
/// <param name="occupiedX">γ operators occupied (X basis)</param>
/// <param name="occupiedZ">γ' operators occupied (Z basis)</param>
public class MajoranaOperator(BitArray occupiedX, BitArray occupiedZ, Coefficient coefficient = Coefficient.PlusI) :
    QuantumOperator(occupiedX, occupiedZ, coefficient)
{
    public override MajoranaOperator Multiply(QuantumOperator other)
    {
        if (other is not MajoranaOperator)
            throw new ArgumentException("Can only multiply MajoranaOperator by another MajoranaOperator.", nameof(other));

        var newOccupiedX = ((BitArray)OccupiedX.Clone()).Xor(other.OccupiedX);
        var newOccupiedZ = ((BitArray)OccupiedZ.Clone()).Xor(other.OccupiedZ);

        var newCoefficient = Coefficient * other.Coefficient;
        bool isExchangeParityOdd = ZippedOccupations().ExchangeParityWith(other.ZippedOccupations());
        if (isExchangeParityOdd)
            newCoefficient *= Coefficient.MinusOne;
        return new MajoranaOperator(newOccupiedX, newOccupiedZ, newCoefficient);
    }

    public override MajoranaOperator Multiply(Coefficient coefficient) =>
        new((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient * coefficient);

    public override MajoranaOperator Negate() =>
        new((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient * Coefficient.MinusOne);

    public override MajoranaOperator Dual() =>
        new((BitArray)OccupiedZ.Clone(), (BitArray)OccupiedX.Clone(), Coefficient);

    public override bool IsHermitian()
    {
        int selfWeight = Weight;
        return selfWeight * (selfWeight - 1) / 2 % 2 == 0 ? Coefficient.IsReal() : Coefficient.IsImaginary();
    }

    public override bool CommutesWith(QuantumOperator other)
    {
        if (other is not MajoranaOperator)
            throw new ArgumentException("Can only check commutation with another MajoranaOperator.", nameof(other));
        int overlapX = BitArray.AndWeight(OccupiedX, other.OccupiedX);
        int overlapZ = BitArray.AndWeight(OccupiedZ, other.OccupiedZ);
        int weightProduct = Weight * other.Weight;
        int totalOverlap = overlapX + overlapZ + weightProduct;
        return totalOverlap % 2 == 0;
    }

    public override MajoranaOperator Clone() =>
        new((BitArray)OccupiedX.Clone(), (BitArray)OccupiedZ.Clone(), Coefficient);

    public static MajoranaOperator CreateHermitian(BitArray occupiedX, BitArray occupiedZ)
    {
        int weight = occupiedX.Weight() + occupiedZ.Weight();
        var coefficient = weight * (weight - 1) / 2 % 2 == 0 ? Coefficient.PlusOne : Coefficient.PlusI;
        return new MajoranaOperator(occupiedX, occupiedZ, coefficient);
    }
}