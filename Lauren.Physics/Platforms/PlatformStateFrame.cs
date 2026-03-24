using System.Buffers;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Platforms;

internal sealed class PlatformStateFrame
{
    private readonly int _fermiColumns;
    private readonly int _qubitColumns;

    public PlatformStateFrame(int totalRows, int qubitColumns, int fermiColumns)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(totalRows);
        ArgumentOutOfRangeException.ThrowIfNegative(qubitColumns);
        ArgumentOutOfRangeException.ThrowIfNegative(fermiColumns);

        _qubitColumns = qubitColumns;
        _fermiColumns = fermiColumns;

        Coefficients = new Coefficient[totalRows];
        QubitRows = new PackedBits[totalRows];
        FermiRows = new PackedBits[totalRows];
        for (int i = 0; i < totalRows; i++)
        {
            QubitRows[i] = new PackedBits(qubitColumns);
            FermiRows[i] = new PackedBits(fermiColumns);
        }
    }

    public int TotalRows => Coefficients.Length;

    public Coefficient[] Coefficients { get; }

    public PackedBits[] QubitRows { get; }

    public PackedBits[] FermiRows { get; }

    public bool CommutesPauli(int row, PackedBits opQubits)
    {
        ValidateRowIndex(row);
        ValidatePauliOperatorDimensions(opQubits);
        return CommutationUtility.CommutesPauli(QubitRows[row], opQubits);
    }

    public void MultiplyRowInPlace(int targetRow, int sourceRow)
    {
        ValidateRowIndex(targetRow);
        ValidateRowIndex(sourceRow);

        Coefficients[targetRow] *= Coefficients[sourceRow];
        QubitRows[targetRow].XorInPlace(QubitRows[sourceRow]);
    }

    public void OverwritePauliRow(int row, PackedBits qubits, Coefficient coefficient)
    {
        ValidateRowIndex(row);
        ValidatePauliOperatorDimensions(qubits);

        Coefficients[row] = coefficient;
        QubitRows[row].Clear();
        QubitRows[row].OrInPlace(qubits);
        if (_fermiColumns != 0)
        {
            FermiRows[row].Clear();
        }
    }

    public bool TrySolvePauliSpan(PackedBits targetQubits, out bool[] solution)
    {
        ValidatePauliOperatorDimensions(targetQubits);

        int rowCount = TotalRows;
        int colCount = targetQubits.Length;
        int rowWordCount = targetQubits.Words.Length;
        int comboWordCount = (rowCount + 63) >> 6;

        ulong[] rentedRows = ArrayPool<ulong>.Shared.Rent(rowCount * rowWordCount);
        ulong[] rentedCombos = ArrayPool<ulong>.Shared.Rent(rowCount * comboWordCount);
        ulong[] rentedTarget = ArrayPool<ulong>.Shared.Rent(rowWordCount);
        try
        {
            var rowBuffer = rentedRows.AsSpan(0, rowCount * rowWordCount);
            var comboBuffer = rentedCombos.AsSpan(0, rowCount * comboWordCount);
            var reducedTarget = rentedTarget.AsSpan(0, rowWordCount);

            for (int row = 0; row < rowCount; row++)
            {
                QubitRows[row].Words.CopyTo(GetRowSlice(rowBuffer, row, rowWordCount));
            }

            comboBuffer.Clear();
            for (int row = 0; row < rowCount; row++)
            {
                SetBit(GetRowSlice(comboBuffer, row, comboWordCount), row);
            }

            targetQubits.Words.CopyTo(reducedTarget);

            int[] pivotRows = new int[rowCount];
            int[] pivotColumns = new int[rowCount];
            int pivotCount = 0;
            int pivotRow = 0;
            for (int col = 0; col < colCount && pivotRow < rowCount; col++)
            {
                int selected = -1;
                for (int row = pivotRow; row < rowCount; row++)
                {
                    if (!GetBit(GetRowSlice(rowBuffer, row, rowWordCount), col))
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
                    SwapRows(rowBuffer, selected, pivotRow, rowWordCount);
                    SwapRows(comboBuffer, selected, pivotRow, comboWordCount);
                }

                ReadOnlySpan<ulong> pivotRowWords = GetRowSlice(rowBuffer, pivotRow, rowWordCount);
                ReadOnlySpan<ulong> pivotComboWords = GetRowSlice(comboBuffer, pivotRow, comboWordCount);
                for (int row = 0; row < rowCount; row++)
                {
                    if (row != pivotRow && GetBit(GetRowSlice(rowBuffer, row, rowWordCount), col))
                    {
                        XorInPlace(GetRowSlice(rowBuffer, row, rowWordCount), pivotRowWords);
                        XorInPlace(GetRowSlice(comboBuffer, row, comboWordCount), pivotComboWords);
                    }
                }

                pivotRows[pivotCount] = pivotRow;
                pivotColumns[pivotCount] = col;
                pivotCount++;
                pivotRow++;
            }

            Span<ulong> reducedCombo = comboWordCount <= 16
                ? stackalloc ulong[comboWordCount]
                : new ulong[comboWordCount];
            reducedCombo.Clear();
            for (int i = 0; i < pivotCount; i++)
            {
                int row = pivotRows[i];
                int col = pivotColumns[i];
                if (!GetBit(reducedTarget, col))
                {
                    continue;
                }

                XorInPlace(reducedTarget, GetRowSlice(rowBuffer, row, rowWordCount));
                XorInPlace(reducedCombo, GetRowSlice(comboBuffer, row, comboWordCount));
            }

            if (!IsZero(reducedTarget))
            {
                solution = Array.Empty<bool>();
                return false;
            }

            solution = new bool[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                solution[i] = GetBit(reducedCombo, i);
            }

            return true;
        }
        finally
        {
            ArrayPool<ulong>.Shared.Return(rentedRows);
            ArrayPool<ulong>.Shared.Return(rentedCombos, clearArray: true);
            ArrayPool<ulong>.Shared.Return(rentedTarget);
        }
    }

    public Coefficient MultiplySelectedCoefficient(IReadOnlyList<bool> selector)
    {
        if (selector.Count != TotalRows)
        {
            throw new ArgumentException("Selector length must match row count.", nameof(selector));
        }

        var accCoefficient = Coefficient.PlusOne;

        for (int i = 0; i < TotalRows; i++)
        {
            if (!selector[i])
            {
                continue;
            }

            accCoefficient *= Coefficients[i];
        }

        return accCoefficient;
    }

    private void ValidateRowIndex(int row)
    {
        if ((uint)row >= (uint)TotalRows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
    }

    private void ValidatePauliOperatorDimensions(PackedBits qubits)
    {
        if (qubits.Length != _qubitColumns)
        {
            throw new ArgumentException("Operator dimensions do not match platform state frame.");
        }
    }

    private static Span<ulong> GetRowSlice(Span<ulong> buffer, int row, int wordCount) =>
        buffer.Slice(row * wordCount, wordCount);

    private static ReadOnlySpan<ulong> GetRowSlice(ReadOnlySpan<ulong> buffer, int row, int wordCount) =>
        buffer.Slice(row * wordCount, wordCount);

    private static bool GetBit(ReadOnlySpan<ulong> words, int index)
    {
        int wordIndex = index >> 6;
        int bitIndex = index & 63;
        return ((words[wordIndex] >> bitIndex) & 1UL) != 0;
    }

    private static void SetBit(Span<ulong> words, int index)
    {
        int wordIndex = index >> 6;
        int bitIndex = index & 63;
        words[wordIndex] |= 1UL << bitIndex;
    }

    private static void XorInPlace(Span<ulong> target, ReadOnlySpan<ulong> source)
    {
        for (int i = 0; i < target.Length; i++)
        {
            target[i] ^= source[i];
        }
    }

    private static void SwapRows(Span<ulong> buffer, int leftRow, int rightRow, int wordCount)
    {
        Span<ulong> left = GetRowSlice(buffer, leftRow, wordCount);
        Span<ulong> right = GetRowSlice(buffer, rightRow, wordCount);
        for (int i = 0; i < wordCount; i++)
        {
            (left[i], right[i]) = (right[i], left[i]);
        }
    }

    private static bool IsZero(ReadOnlySpan<ulong> words)
    {
        foreach (ulong word in words)
        {
            if (word != 0)
            {
                return false;
            }
        }

        return true;
    }
}
