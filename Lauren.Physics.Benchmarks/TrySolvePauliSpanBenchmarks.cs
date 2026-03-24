using System.Collections;
using BenchmarkDotNet.Attributes;
using Lauren.Physics.Platforms;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Benchmarks;

public abstract class TrySolvePauliSpanBenchmarkBase
{
    private readonly Random _random = new(12345);
    private PlatformStateFrame _frame = null!;
    private PackedBits _solvableTarget = null!;
    private PackedBits _unsolvableTarget = null!;

    [Params(16, 64)]
    public int Rows { get; set; }

    [Params(64, 256)]
    public int Columns { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _frame = new PlatformStateFrame(totalRows: Rows, qubitColumns: Columns, fermiColumns: 0);

        for (int row = 0; row < Rows; row++)
        {
            _frame.Coefficients[row] = Coefficient.PlusOne;
            _frame.QubitRows[row] = RandomRow();
        }

        _solvableTarget = BuildSolvableTarget();
        _unsolvableTarget = _solvableTarget.Clone();
        _unsolvableTarget[Columns - 1] = true;
    }

    protected int LegacySolveSolvable()
    {
        bool solved = TrySolvePauliSpanLegacy(_frame, _solvableTarget, out bool[] solution);
        return solved ? solution.Length : -1;
    }

    protected int CurrentSolveSolvable()
    {
        bool solved = _frame.TrySolvePauliSpan(_solvableTarget, out bool[] solution);
        return solved ? solution.Length : -1;
    }

    protected int LegacySolveUnsolvable()
    {
        bool solved = TrySolvePauliSpanLegacy(_frame, _unsolvableTarget, out bool[] solution);
        return solved ? solution.Length : -1;
    }

    protected int CurrentSolveUnsolvable()
    {
        bool solved = _frame.TrySolvePauliSpan(_unsolvableTarget, out bool[] solution);
        return solved ? solution.Length : -1;
    }

    private PackedBits RandomRow()
    {
        var bits = new BitArray(Columns);
        for (int col = 0; col < Columns - 1; col++)
        {
            bits[col] = _random.NextDouble() < 0.25;
        }

        return new PackedBits(bits);
    }

    private PackedBits BuildSolvableTarget()
    {
        var target = new PackedBits(Columns);
        for (int row = 0; row < Rows; row++)
        {
            if (_random.Next(2) == 0)
            {
                continue;
            }

            target.XorInPlace(_frame.QubitRows[row]);
        }

        return target;
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

[MemoryDiagnoser]
public class TrySolvePauliSpanSolvableBenchmarks : TrySolvePauliSpanBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public int LegacyTrySolvePauliSpan() => LegacySolveSolvable();

    [Benchmark]
    public int TrySolvePauliSpan() => CurrentSolveSolvable();
}

[MemoryDiagnoser]
public class TrySolvePauliSpanUnsolvableBenchmarks : TrySolvePauliSpanBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public int LegacyTrySolvePauliSpan() => LegacySolveUnsolvable();

    [Benchmark]
    public int TrySolvePauliSpan() => CurrentSolveUnsolvable();
}
