using System.Collections;
using Lauren.Physics.Operators;
using Xunit;

namespace Lauren.Physics.Tests;

public class MajoranaOperatorTests
{
    [Fact]
    public void Multiply_WithOperator_ParityOdd_FlipsSign()
    {
        var leftX = new BitArray(1);
        var leftZ = new BitArray(1) { [0] = true };
        var left = new MajoranaOperator(leftX, leftZ, Coefficient.PlusI);

        var rightX = new BitArray(1) { [0] = true };
        var rightZ = new BitArray(1);
        var right = new MajoranaOperator(rightX, rightZ, Coefficient.PlusOne);

        var leftCopyX = new BitArray(left.OccupiedX);
        var leftCopyZ = new BitArray(left.OccupiedZ);
        var rightCopyX = new BitArray(right.OccupiedX);
        var rightCopyZ = new BitArray(right.OccupiedZ);

        var result = left.Multiply(right);

        var op = Assert.IsType<MajoranaOperator>(result);
        Assert.Equal(Coefficient.MinusI, op.Coefficient);
        Assert.True(BitsEqual(new BitArray(1) { [0] = true }, op.OccupiedX));
        Assert.True(BitsEqual(new BitArray(1) { [0] = true }, op.OccupiedZ));
        Assert.True(BitsEqual(leftCopyX, left.OccupiedX));
        Assert.True(BitsEqual(leftCopyZ, left.OccupiedZ));
        Assert.True(BitsEqual(rightCopyX, right.OccupiedX));
        Assert.True(BitsEqual(rightCopyZ, right.OccupiedZ));
    }

    [Fact]
    public void Multiply_WithOperator_ParityEven_DoesNotFlipSign()
    {
        var leftX = new BitArray(2) { [0] = true };
        var leftZ = new BitArray(2);
        var left = new MajoranaOperator(leftX, leftZ, Coefficient.PlusI);

        var rightX = new BitArray(2) { [1] = true };
        var rightZ = new BitArray(2);
        var right = new MajoranaOperator(rightX, rightZ, Coefficient.MinusOne);

        var result = left.Multiply(right);

        var op = Assert.IsType<MajoranaOperator>(result);
        Assert.Equal(Coefficient.MinusI, op.Coefficient);
        Assert.True(BitsEqual(new BitArray(2) { [0] = true, [1] = true }, op.OccupiedX));
        Assert.True(BitsEqual(new BitArray(2), op.OccupiedZ));
    }

    [Fact]
    public void Multiply_WithCoefficient_MultipliesCoefficientAndClonesBits()
    {
        var occupiedX = new BitArray(3) { [1] = true };
        var occupiedZ = new BitArray(3) { [2] = true };
        var op = new MajoranaOperator(occupiedX, occupiedZ, Coefficient.PlusI);

        var result = op.Multiply(Coefficient.MinusOne);

        var majorana = Assert.IsType<MajoranaOperator>(result);
        Assert.Equal(Coefficient.MinusI, majorana.Coefficient);
        Assert.True(BitsEqual(occupiedX, majorana.OccupiedX));
        Assert.True(BitsEqual(occupiedZ, majorana.OccupiedZ));
        Assert.False(ReferenceEquals(op.OccupiedX, majorana.OccupiedX));
        Assert.False(ReferenceEquals(op.OccupiedZ, majorana.OccupiedZ));
    }

    [Fact]
    public void Negate_MultipliesByMinusOne()
    {
        var op = new MajoranaOperator(new BitArray(1), new BitArray(1), Coefficient.PlusI);

        var result = op.Negate();

        var majorana = Assert.IsType<MajoranaOperator>(result);
        Assert.Equal(Coefficient.MinusI, majorana.Coefficient);
    }

    [Fact]
    public void Dual_SwapsOccupiedXAndZ()
    {
        var occupiedX = new BitArray(2) { [0] = true };
        var occupiedZ = new BitArray(2) { [1] = true };
        var op = new MajoranaOperator(occupiedX, occupiedZ, Coefficient.MinusOne);

        var result = op.Dual();

        var majorana = Assert.IsType<MajoranaOperator>(result);
        Assert.True(BitsEqual(occupiedZ, majorana.OccupiedX));
        Assert.True(BitsEqual(occupiedX, majorana.OccupiedZ));
        Assert.Equal(Coefficient.MinusOne, majorana.Coefficient);
    }

    [Theory]
    [MemberData(nameof(IsHermitianCases))]
    public void IsHermitian_UsesWeightParity(bool[] xBits, bool[] zBits, Coefficient coefficient, bool expected)
    {
        var op = new MajoranaOperator(new BitArray(xBits), new BitArray(zBits), coefficient);

        var actual = op.IsHermitian();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(CommutesWithCases))]
    public void CommutesWith_ReturnsExpected(bool[] leftX, bool[] leftZ, bool[] rightX, bool[] rightZ, bool expected)
    {
        var left = new MajoranaOperator(new BitArray(leftX), new BitArray(leftZ), Coefficient.PlusOne);
        var right = new MajoranaOperator(new BitArray(rightX), new BitArray(rightZ), Coefficient.PlusOne);

        var actual = left.CommutesWith(right);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CommutesWith_WrongType_Throws()
    {
        var left = new MajoranaOperator(new BitArray(1), new BitArray(1));
        var right = new PauliOperator(new BitArray(1), new BitArray(1));

        Assert.Throws<ArgumentException>(() => left.CommutesWith(right));
    }

    [Fact]
    public void Clone_CopiesValues()
    {
        var occupiedX = new BitArray(3) { [0] = true };
        var occupiedZ = new BitArray(3) { [2] = true };
        var op = new MajoranaOperator(occupiedX, occupiedZ, Coefficient.PlusOne);

        var clone = op.Clone();

        var majorana = Assert.IsType<MajoranaOperator>(clone);
        Assert.Equal(Coefficient.PlusOne, majorana.Coefficient);
        Assert.True(BitsEqual(occupiedX, majorana.OccupiedX));
        Assert.True(BitsEqual(occupiedZ, majorana.OccupiedZ));
        Assert.False(ReferenceEquals(op.OccupiedX, majorana.OccupiedX));
        Assert.False(ReferenceEquals(op.OccupiedZ, majorana.OccupiedZ));
    }

    [Fact]
    public void CreateHermitian_SetsCorrectCoefficient_AndReturnsHermitianOperator()
    {
        var occupiedX = new BitArray(2) { [0] = true };
        var occupiedZ = new BitArray(2) { [1] = true };
        var hermitianOp = MajoranaOperator.CreateHermitian(occupiedX, occupiedZ);
        var majorana = Assert.IsType<MajoranaOperator>(hermitianOp);
        Assert.True(BitsEqual(occupiedX, majorana.OccupiedX));
        Assert.True(BitsEqual(occupiedZ, majorana.OccupiedZ));
        Assert.Equal(Coefficient.PlusI, majorana.Coefficient);
        Assert.True(majorana.IsHermitian());
    }

    public static IEnumerable<object[]> IsHermitianCases()
    {
        yield return [new[] { true }, new[] { false }, Coefficient.PlusOne, true];
        yield return [new[] { true }, new[] { false }, Coefficient.PlusI, false];
        yield return [new[] { true, true }, new[] { false, false }, Coefficient.PlusI, true];
        yield return [new[] { true, true }, new[] { false, false }, Coefficient.PlusOne, false];
    }

    public static IEnumerable<object[]> CommutesWithCases()
    {
        yield return
        [
            Array.Empty<bool>(), Array.Empty<bool>(),
            Array.Empty<bool>(), Array.Empty<bool>(),
            true
        ];
        yield return
        [
            new[] { true }, new[] { false },
            new[] { false }, new[] { true },
            false
        ];
        yield return
        [
            new[] { true }, new[] { false },
            new[] { true }, new[] { false },
            true
        ];
        yield return
        [
            new[] { true, true }, new[] { false, false },
            new[] { false, true }, new[] { false, false },
            false
        ];
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
