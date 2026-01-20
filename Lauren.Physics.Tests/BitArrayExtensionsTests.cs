using System.Collections;
using Lauren.Physics.Utility;
using Xunit;

namespace Lauren.Physics.Tests;

public class BitArrayExtensionsTests
{
    [Fact]
    public void Weight_Null_Throws()
    {
        BitArray? bits = null;

        Assert.Throws<ArgumentNullException>(() => bits!.Weight());
    }

    [Fact]
    public void Weight_Empty_ReturnsZero()
    {
        var bits = new BitArray(0);

        Assert.Equal(0, bits.Weight());
    }

    [Fact]
    public void Weight_ReturnsExpectedCount()
    {
        var bits = new BitArray([
            true, false, false, true, false,
            false, true, false, false, true
        ]);

        Assert.Equal(4, bits.Weight());
    }

    [Fact]
    public void OrWeight_LengthMismatch_Throws()
    {
        var left = new BitArray(5);
        var right = new BitArray(6);

        Assert.Throws<ArgumentException>(() => BitArray.OrWeight(left, right));
    }

    [Fact]
    public void OrWeight_ReturnsExpectedCount_AndDoesNotMutateInputs()
    {
        var left = new BitArray(10)
        {
            [0] = true,
            [1] = true,
            [9] = true
        };

        var right = new BitArray(10)
        {
            [1] = true,
            [2] = true,
            [9] = true
        };

        var leftCopy = new BitArray(left);
        var rightCopy = new BitArray(right);

        var weight = BitArray.OrWeight(left, right);

        Assert.Equal(4, weight);
        Assert.True(BitsEqual(left, leftCopy));
        Assert.True(BitsEqual(right, rightCopy));
    }

    [Fact]
    public void AndWeight_LengthMismatch_Throws()
    {
        var left = new BitArray(5);
        var right = new BitArray(6);

        Assert.Throws<ArgumentException>(() => BitArray.AndWeight(left, right));
    }

    [Fact]
    public void AndWeight_ReturnsExpectedCount_AndDoesNotMutateInputs()
    {
        var left = new BitArray(10)
        {
            [0] = true,
            [1] = true,
            [9] = true
        };

        var right = new BitArray(10)
        {
            [1] = true,
            [2] = true,
            [9] = true
        };

        var leftCopy = new BitArray(left);
        var rightCopy = new BitArray(right);

        var weight = BitArray.AndWeight(left, right);

        Assert.Equal(2, weight);
        Assert.True(BitsEqual(left, leftCopy));
        Assert.True(BitsEqual(right, rightCopy));
    }

    [Fact]
    public void ValueEquals_BothNull_ReturnsTrue()
    {
        Assert.True(BitArray.ValueEquals(null, null));
    }

    [Fact]
    public void ValueEquals_SameReference_ReturnsTrue()
    {
        var bits = new BitArray(3, true);

        Assert.True(BitArray.ValueEquals(bits, bits));
    }

    [Fact]
    public void ValueEquals_DifferentLengths_ReturnsFalse()
    {
        var left = new BitArray(2);
        var right = new BitArray(3);

        Assert.False(BitArray.ValueEquals(left, right));
    }

    [Fact]
    public void ValueEquals_SameValues_ReturnsTrue()
    {
        var left = new BitArray([true, false, true, false]);
        var right = new BitArray([true, false, true, false]);

        Assert.True(BitArray.ValueEquals(left, right));
    }

    [Fact]
    public void ValueEquals_DifferentValues_ReturnsFalse()
    {
        var left = new BitArray([true, false, true, false]);
        var right = new BitArray([true, true, true, false]);

        Assert.False(BitArray.ValueEquals(left, right));
    }

    [Fact]
    public void ValueEquals_NullAndNonNull_ReturnsFalse()
    {
        var bits = new BitArray(1, true);

        Assert.False(BitArray.ValueEquals(bits, null));
        Assert.False(BitArray.ValueEquals(null, bits));
    }

    [Theory]
    [MemberData(nameof(ExchangeParityWithCases))]
    public void ExchangeParityWith_ReturnsExpected(bool[] selfBits, bool[] otherBits)
    {
        var self = new BitArray(selfBits);
        var other = new BitArray(otherBits);

        var expected = ExchangeParityReference(self, other);
        var actual = self.ExchangeParityWith(other);

        Assert.Equal(expected, actual);
    }

    private static bool BitsEqual(BitArray left, BitArray right)
    {
        if (left.Length != right.Length) return false;

        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;

        return true;
    }

    public static IEnumerable<object[]> ExchangeParityWithCases()
    {
        yield return [Array.Empty<bool>(), Array.Empty<bool>()];
        yield return [new[] { false, false, false }, new[] { false, false, false }];
        yield return [new[] { true }, new[] { true }];
        yield return [new[] { true, false, false }, new[] { false, true, false }];
        yield return [new[] { false, false, true }, new[] { true, false, false }];
        yield return [new[] { true, false, true, false }, new[] { true, true, false, false }];
        yield return
        [
            new[] { false, true, false, false, true },
            new[] { true, false, false, true, false }
        ];
        yield return [new[] { true, true, true }, new[] { true, true, true }];
    }

    /// <summary>
    ///     A slow path reference implementation of ExchangeParityWith for testing.
    /// </summary>
    private static bool ExchangeParityReference(BitArray self, BitArray other)
    {
        int length = self.Length;
        int count = 0;

        for (int b = 0; b < length; b++)
        {
            if (!other[b]) continue;

            for (int a = b + 1; a < length; a++)
            {
                if (self[a]) count++;
            }
        }

        return (count & 1) == 1;
    }
}
