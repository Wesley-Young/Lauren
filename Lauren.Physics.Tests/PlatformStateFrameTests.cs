using System.Collections;
using Lauren.Physics.Platforms;
using Lauren.Physics.Utility;
using Xunit;

namespace Lauren.Physics.Tests;

public class PlatformStateFrameTests
{
    [Fact]
    public void TrySolvePauliSpan_MatchesExhaustiveSpanMembershipOnRandomInputs()
    {
        var random = new Random(12345);

        for (int iteration = 0; iteration < 200; iteration++)
        {
            int rowCount = random.Next(1, 6);
            int columnCount = random.Next(1, 10);
            var frame = new PlatformStateFrame(totalRows: rowCount, qubitColumns: columnCount, fermiColumns: 0);

            for (int row = 0; row < rowCount; row++)
            {
                frame.Coefficients[row] = Coefficient.PlusOne;
                frame.QubitRows[row] = Packed(RandomBits(random, columnCount));
            }

            bool[] targetBits = RandomBits(random, columnCount);
            bool solved = frame.TrySolvePauliSpan(Packed(targetBits), out bool[] solution);

            bool expectedSolved = TrySolveExhaustively(frame.QubitRows, targetBits, out bool[] expectedSolution);

            Assert.Equal(expectedSolved, solved);
            if (!solved)
            {
                Assert.Empty(solution);
                continue;
            }

            Assert.Equal(expectedSolution, solution);
        }
    }

    [Fact]
    public void TrySolvePauliSpan_PicksLowestAvailablePivotColumn()
    {
        var frame = new PlatformStateFrame(totalRows: 2, qubitColumns: 6, fermiColumns: 0);
        frame.Coefficients[0] = Coefficient.PlusOne;
        frame.Coefficients[1] = Coefficient.PlusOne;
        frame.QubitRows[0] = Packed([false, false, false, false, true, false]);
        frame.QubitRows[1] = Packed([false, true, false, false, false, false]);

        bool solved = frame.TrySolvePauliSpan(
            Packed([false, true, false, false, false, false]),
            out bool[] solution);

        Assert.True(solved);
        Assert.Equal([false, true], solution);
    }

    [Fact]
    public void TrySolvePauliSpan_FindsLinearCombinationAcrossMultipleRows()
    {
        var frame = new PlatformStateFrame(totalRows: 3, qubitColumns: 6, fermiColumns: 0);
        frame.Coefficients[0] = Coefficient.PlusOne;
        frame.Coefficients[1] = Coefficient.PlusOne;
        frame.Coefficients[2] = Coefficient.PlusOne;
        frame.QubitRows[0] = Packed([true, false, false, true, false, false]);
        frame.QubitRows[1] = Packed([false, true, false, false, false, false]);
        frame.QubitRows[2] = Packed([false, false, true, true, false, false]);

        bool solved = frame.TrySolvePauliSpan(
            Packed([true, false, true, false, false, false]),
            out bool[] solution);

        Assert.True(solved);
        Assert.Equal([true, false, true], solution);
    }

    [Fact]
    public void TrySolvePauliSpan_ReturnsFalseWhenTargetNotInSpan()
    {
        var frame = new PlatformStateFrame(totalRows: 2, qubitColumns: 4, fermiColumns: 0);
        frame.Coefficients[0] = Coefficient.PlusOne;
        frame.Coefficients[1] = Coefficient.PlusOne;
        frame.QubitRows[0] = Packed([true, false, false, false]);
        frame.QubitRows[1] = Packed([false, true, false, false]);

        bool solved = frame.TrySolvePauliSpan(
            Packed([false, false, true, false]),
            out bool[] solution);

        Assert.False(solved);
        Assert.Empty(solution);
    }

    private static PackedBits Packed(bool[] bits) => new(new BitArray(bits));

    private static bool[] RandomBits(Random random, int length)
    {
        var bits = new bool[length];
        for (int i = 0; i < length; i++)
        {
            bits[i] = random.Next(2) == 1;
        }

        return bits;
    }

    private static bool TrySolveExhaustively(PackedBits[] rows, bool[] target, out bool[] solution)
    {
        int rowCount = rows.Length;
        int combinationCount = 1 << rowCount;
        for (int mask = 0; mask < combinationCount; mask++)
        {
            var acc = new bool[target.Length];
            var candidate = new bool[rowCount];
            for (int row = 0; row < rowCount; row++)
            {
                if (((mask >> row) & 1) == 0)
                {
                    continue;
                }

                candidate[row] = true;
                for (int col = 0; col < target.Length; col++)
                {
                    acc[col] ^= rows[row][col];
                }
            }

            if (!acc.AsSpan().SequenceEqual(target))
            {
                continue;
            }

            solution = candidate;
            return true;
        }

        solution = Array.Empty<bool>();
        return false;
    }
}
