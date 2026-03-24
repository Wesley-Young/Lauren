using System.Collections;
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
        QubitRows = new BitArray[totalRows];
        FermiRows = new BitArray[totalRows];
        for (int i = 0; i < totalRows; i++)
        {
            QubitRows[i] = new BitArray(qubitColumns);
            FermiRows[i] = new BitArray(fermiColumns);
        }
    }

    public int TotalRows => Coefficients.Length;

    public Coefficient[] Coefficients { get; }

    public BitArray[] QubitRows { get; }

    public BitArray[] FermiRows { get; }

    public bool CommutesPauli(int row, BitArray opQubits)
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
        QubitRows[targetRow].Xor(QubitRows[sourceRow]);
    }

    public void OverwritePauliRow(int row, BitArray qubits, Coefficient coefficient)
    {
        ValidateRowIndex(row);
        ValidatePauliOperatorDimensions(qubits);

        Coefficients[row] = coefficient;
        QubitRows[row].SetAll(false);
        QubitRows[row].Or(qubits);
        if (_fermiColumns != 0)
        {
            FermiRows[row].SetAll(false);
        }
    }

    public bool TrySolvePauliSpan(BitArray targetQubits, out bool[] solution)
    {
        ValidatePauliOperatorDimensions(targetQubits);

        int rowCount = TotalRows;
        int colCount = targetQubits.Length;

        var rows = new BitArray[rowCount];
        var combos = new BitArray[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            rows[i] = new BitArray(QubitRows[i]);
            combos[i] = new BitArray(rowCount)
            {
                [i] = true
            };
        }

        var pivotRows = new List<int>();
        var pivotColumns = new List<int>();

        int pivotRow = 0;
        for (int col = 0; col < colCount && pivotRow < rowCount; col++)
        {
            int selected = -1;
            for (int row = pivotRow; row < rowCount; row++)
            {
                if (rows[row][col])
                {
                    selected = row;
                    break;
                }
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
                    rows[row].Xor(rows[pivotRow]);
                    combos[row].Xor(combos[pivotRow]);
                }
            }

            pivotRows.Add(pivotRow);
            pivotColumns.Add(col);
            pivotRow++;
        }

        var reducedTarget = new BitArray(targetQubits);
        var reducedCombo = new BitArray(rowCount);
        for (int i = 0; i < pivotRows.Count; i++)
        {
            int row = pivotRows[i];
            int col = pivotColumns[i];
            if (!reducedTarget[col])
            {
                continue;
            }

            reducedTarget.Xor(rows[row]);
            reducedCombo.Xor(combos[row]);
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

    private void ValidatePauliOperatorDimensions(BitArray qubits)
    {
        if (qubits.Length != _qubitColumns)
        {
            throw new ArgumentException("Operator dimensions do not match platform state frame.");
        }
    }
}
