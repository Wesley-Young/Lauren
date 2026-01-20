using System.Collections;
using Lauren.Physics.Operators;
using Xunit;

namespace Lauren.Physics.Tests;

public class PauliOperatorTests
{
    [Fact]
    public void Multiply_WithOperator_XorAndCoefficient()
    {
        var leftX = new BitArray(4) { [0] = true, [2] = true };
        var leftZ = new BitArray(4) { [1] = true };
        var left = new PauliOperator(leftX, leftZ, Coefficient.PlusOne);

        var rightX = new BitArray(4) { [2] = true, [3] = true };
        var rightZ = new BitArray(4) { [1] = true, [2] = true };
        var right = new PauliOperator(rightX, rightZ, Coefficient.MinusI);

        var result = left.Multiply(right);

        var expectedX = new BitArray(4) { [0] = true, [3] = true };
        var expectedZ = new BitArray(4) { [2] = true };

        var pauli = Assert.IsType<PauliOperator>(result);
        Assert.Equal(Coefficient.MinusI, pauli.Coefficient);
        Assert.True(BitsEqual(expectedX, pauli.OccupiedX));
        Assert.True(BitsEqual(expectedZ, pauli.OccupiedZ));
    }

    [Fact]
    public void Multiply_WithCoefficient_MultipliesCoefficientAndClonesBits()
    {
        var occupiedX = new BitArray(3) { [1] = true };
        var occupiedZ = new BitArray(3) { [2] = true };
        var op = new PauliOperator(occupiedX, occupiedZ, Coefficient.PlusI);

        var result = op.Multiply(Coefficient.MinusOne);

        var pauli = Assert.IsType<PauliOperator>(result);
        Assert.Equal(Coefficient.MinusI, pauli.Coefficient);
        Assert.True(BitsEqual(occupiedX, pauli.OccupiedX));
        Assert.True(BitsEqual(occupiedZ, pauli.OccupiedZ));
        Assert.False(ReferenceEquals(op.OccupiedX, pauli.OccupiedX));
        Assert.False(ReferenceEquals(op.OccupiedZ, pauli.OccupiedZ));
    }

    [Fact]
    public void Negate_MultipliesByMinusOne()
    {
        var op = new PauliOperator(new BitArray(1), new BitArray(1), Coefficient.PlusI);

        var result = op.Negate();

        var pauli = Assert.IsType<PauliOperator>(result);
        Assert.Equal(Coefficient.MinusI, pauli.Coefficient);
    }

    [Fact]
    public void Dual_SwapsOccupiedXAndZ()
    {
        var occupiedX = new BitArray(2) { [0] = true };
        var occupiedZ = new BitArray(2) { [1] = true };
        var op = new PauliOperator(occupiedX, occupiedZ, Coefficient.MinusOne);

        var result = op.Dual();

        var pauli = Assert.IsType<PauliOperator>(result);
        Assert.True(BitsEqual(occupiedZ, pauli.OccupiedX));
        Assert.True(BitsEqual(occupiedX, pauli.OccupiedZ));
        Assert.Equal(Coefficient.MinusOne, pauli.Coefficient);
    }

    [Fact]
    public void IsHermitian_UsesOverlapParity()
    {
        var oddOverlapX = new BitArray(2) { [0] = true };
        var oddOverlapZ = new BitArray(2) { [0] = true };
        var oddImaginary = new PauliOperator(oddOverlapX, oddOverlapZ, Coefficient.PlusI);
        var oddReal = new PauliOperator(oddOverlapX, oddOverlapZ, Coefficient.PlusOne);

        Assert.True(oddImaginary.IsHermitian());
        Assert.False(oddReal.IsHermitian());

        var evenOverlapX = new BitArray(2) { [0] = true, [1] = true };
        var evenOverlapZ = new BitArray(2) { [0] = true, [1] = true };
        var evenReal = new PauliOperator(evenOverlapX, evenOverlapZ, Coefficient.MinusOne);
        var evenImaginary = new PauliOperator(evenOverlapX, evenOverlapZ, Coefficient.MinusI);

        Assert.True(evenReal.IsHermitian());
        Assert.False(evenImaginary.IsHermitian());
    }

    [Fact]
    public void Clone_CopiesValues()
    {
        var occupiedX = new BitArray(3) { [0] = true };
        var occupiedZ = new BitArray(3) { [2] = true };
        var op = new PauliOperator(occupiedX, occupiedZ, Coefficient.PlusOne);

        var clone = op.Clone();

        var pauli = Assert.IsType<PauliOperator>(clone);
        Assert.Equal(Coefficient.PlusOne, pauli.Coefficient);
        Assert.True(BitsEqual(occupiedX, pauli.OccupiedX));
        Assert.True(BitsEqual(occupiedZ, pauli.OccupiedZ));
        Assert.False(ReferenceEquals(op.OccupiedX, pauli.OccupiedX));
        Assert.False(ReferenceEquals(op.OccupiedZ, pauli.OccupiedZ));
    }

    private static bool BitsEqual(BitArray left, BitArray right)
    {
        if (left.Length != right.Length) return false;

        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;

        return true;
    }
}
