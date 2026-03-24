using System.Collections;
using System.Diagnostics;
using Lauren.Physics.Platforms;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Benchmarks;

internal static class ManualTrySolvePauliSpanComparison
{
    public static void Run()
    {
        Console.WriteLine("Scenario,Rows,Columns,LegacyUs,CurrentUs,Speedup,LegacyAllocB,CurrentAllocB,AllocRatio");

        RunScenario("Solvable", 16, 64);
        RunScenario("Solvable", 16, 256);
        RunScenario("Solvable", 64, 64);
        RunScenario("Solvable", 64, 256);
        RunScenario("Unsolvable", 16, 64);
        RunScenario("Unsolvable", 16, 256);
        RunScenario("Unsolvable", 64, 64);
        RunScenario("Unsolvable", 64, 256);
    }

    private static void RunScenario(string scenario, int rows, int columns)
    {
        var fixture = new TrySolveFixture(rows, columns);
        PackedBits target = scenario == "Solvable"
            ? fixture.SolvableTarget
            : fixture.UnsolvableTarget;

        int iterations = rows <= 16 ? 200_000 : 25_000;

        for (int i = 0; i < 1_000; i++)
        {
            TrySolvePauliSpanLegacy(fixture.Frame, target, out _);
            fixture.Frame.TrySolvePauliSpan(target, out _);
        }

        (double legacyUs, long legacyAlloc) = Measure(
            () => TrySolvePauliSpanLegacy(fixture.Frame, target, out _),
            iterations);
        (double currentUs, long currentAlloc) = Measure(
            () => fixture.Frame.TrySolvePauliSpan(target, out _),
            iterations);

        Console.WriteLine(
            "{0},{1},{2},{3:F3},{4:F3},{5:F2}x,{6},{7},{8:F2}x",
            scenario,
            rows,
            columns,
            legacyUs,
            currentUs,
            legacyUs / currentUs,
            legacyAlloc,
            currentAlloc,
            (double)legacyAlloc / currentAlloc);
    }

    private static (double microsecondsPerOp, long bytesPerOp) Measure(Action action, int iterations)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }

        long elapsed = Stopwatch.GetTimestamp() - start;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        return (
            elapsed * 1_000_000.0 / Stopwatch.Frequency / iterations,
            allocated / iterations);
    }

    private sealed class TrySolveFixture
    {
        private readonly Random _random = new(12345);

        public TrySolveFixture(int rows, int columns)
        {
            Frame = new PlatformStateFrame(totalRows: rows, qubitColumns: columns, fermiColumns: 0);
            for (int row = 0; row < rows; row++)
            {
                Frame.Coefficients[row] = Coefficient.PlusOne;
                Frame.QubitRows[row] = RandomRow(columns);
            }

            SolvableTarget = BuildSolvableTarget(rows, columns);
            UnsolvableTarget = SolvableTarget.Clone();
            UnsolvableTarget[columns - 1] = true;
        }

        public PlatformStateFrame Frame { get; }

        public PackedBits SolvableTarget { get; }

        public PackedBits UnsolvableTarget { get; }

        private PackedBits RandomRow(int columns)
        {
            var bits = new BitArray(columns);
            for (int col = 0; col < columns - 1; col++)
            {
                bits[col] = _random.NextDouble() < 0.25;
            }

            return new PackedBits(bits);
        }

        private PackedBits BuildSolvableTarget(int rows, int columns)
        {
            var target = new PackedBits(columns);
            for (int row = 0; row < rows; row++)
            {
                if (_random.Next(2) == 0)
                {
                    continue;
                }

                target.XorInPlace(Frame.QubitRows[row]);
            }

            return target;
        }
    }

    private static bool TrySolvePauliSpanLegacy(PlatformStateFrame frame, PackedBits targetQubits, out bool[] solution)
    {
        int rowCount = frame.TotalRows;
        int colCount = targetQubits.Length;

        var rows = new PackedBits[rowCount];
        var combos = new PackedBits[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            rows[i] = frame.QubitRows[i].Clone();
            combos[i] = new PackedBits(rowCount);
            combos[i][i] = true;
        }

        int[] pivotRows = new int[rowCount];
        int[] pivotColumns = new int[rowCount];
        int pivotCount = 0;
        int pivotRow = 0;
        for (int col = 0; col < colCount && pivotRow < rowCount; col++)
        {
            int selected = -1;
            for (int row = pivotRow; row < rowCount; row++)
            {
                if (!rows[row][col])
                {
                    continue;
                }

                selected = row;
                break;
            }

            if (selected == -1)
            {
                continue;
            }

            if (selected != pivotRow)
            {
                (rows[selected], rows[pivotRow]) = (rows[pivotRow], rows[selected]);
                (combos[selected], combos[pivotRow]) = (combos[pivotRow], combos[selected]);
            }

            for (int row = 0; row < rowCount; row++)
            {
                if (row != pivotRow && rows[row][col])
                {
                    rows[row].XorInPlace(rows[pivotRow]);
                    combos[row].XorInPlace(combos[pivotRow]);
                }
            }

            pivotRows[pivotCount] = pivotRow;
            pivotColumns[pivotCount] = col;
            pivotCount++;
            pivotRow++;
        }

        var reducedTarget = targetQubits.Clone();
        var reducedCombo = new PackedBits(rowCount);
        for (int i = 0; i < pivotCount; i++)
        {
            int row = pivotRows[i];
            int col = pivotColumns[i];
            if (!reducedTarget[col])
            {
                continue;
            }

            reducedTarget.XorInPlace(rows[row]);
            reducedCombo.XorInPlace(combos[row]);
        }

        if (reducedTarget.Weight() != 0)
        {
            solution = Array.Empty<bool>();
            return false;
        }

        solution = new bool[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            solution[i] = reducedCombo[i];
        }

        return true;
    }
}
