using System.Collections;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Platforms;

internal sealed class PlatformStateFrame
{
    private readonly int _fermiColumns;
    private readonly int _qubitColumns;

    public PlatformStateFrame(int totalRows, int qubitColumns, int fermiColumns)
    {
        if (totalRows < 0) throw new ArgumentOutOfRangeException(nameof(totalRows));
        if (qubitColumns < 0) throw new ArgumentOutOfRangeException(nameof(qubitColumns));
        if (fermiColumns < 0) throw new ArgumentOutOfRangeException(nameof(fermiColumns));

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

    public bool Commutes(int row, BitArray opQubits, BitArray opFermiSites)
    {
        ValidateRowIndex(row);
        ValidateOperatorDimensions(opQubits, opFermiSites);

        bool parity = false;
        var rowQubits = QubitRows[row];
        for (int i = 0; i < rowQubits.Length; i += 2)
        {
            if (rowQubits[i] && opQubits[i + 1]) parity = !parity;
            if (rowQubits[i + 1] && opQubits[i]) parity = !parity;
        }

        bool rowFermiWeightOdd = false;
        bool opFermiWeightOdd = false;
        var rowFermiSites = FermiRows[row];
        for (int i = 0; i < rowFermiSites.Length; i++)
        {
            if (rowFermiSites[i] && opFermiSites[i]) parity = !parity;
            if (rowFermiSites[i]) rowFermiWeightOdd = !rowFermiWeightOdd;
            if (opFermiSites[i]) opFermiWeightOdd = !opFermiWeightOdd;
        }

        if (rowFermiWeightOdd && opFermiWeightOdd) parity = !parity;
        return !parity;
    }

    public void MultiplyRowInPlace(int targetRow, int sourceRow)
    {
        ValidateRowIndex(targetRow);
        ValidateRowIndex(sourceRow);

        var coefficient = Coefficients[targetRow] * Coefficients[sourceRow];
        if (FermiRows[targetRow].ExchangeParityWith(FermiRows[sourceRow]))
        {
            coefficient *= Coefficient.MinusOne;
        }

        QubitRows[targetRow].Xor(QubitRows[sourceRow]);
        FermiRows[targetRow].Xor(FermiRows[sourceRow]);
        Coefficients[targetRow] = coefficient;
    }

    public void OverwriteRow(int row, BitArray qubits, BitArray fermiSites, Coefficient coefficient)
    {
        ValidateRowIndex(row);
        ValidateOperatorDimensions(qubits, fermiSites);

        Coefficients[row] = coefficient;
        QubitRows[row].SetAll(false);
        QubitRows[row].Or(qubits);
        FermiRows[row].SetAll(false);
        FermiRows[row].Or(fermiSites);
    }

    public bool TrySolveSpan(BitArray targetQubits, BitArray targetFermiSites, out bool[] solution)
    {
        ValidateOperatorDimensions(targetQubits, targetFermiSites);

        int rowCount = TotalRows;
        int colCount = targetFermiSites.Length + targetQubits.Length;

        var rows = new BitArray[rowCount];
        var combos = new BitArray[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            rows[i] = Flatten(QubitRows[i], FermiRows[i]);
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

        var reducedTarget = Flatten(targetQubits, targetFermiSites);
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

        var accQubits = new BitArray(_qubitColumns);
        var accFermiSites = new BitArray(_fermiColumns);
        var accCoefficient = Coefficient.PlusOne;

        for (int i = 0; i < TotalRows; i++)
        {
            if (!selector[i])
            {
                continue;
            }

            MultiplyAccumulatorWithRow(ref accCoefficient, accQubits, accFermiSites, i);
        }

        return accCoefficient;
    }

    private void MultiplyAccumulatorWithRow(
        ref Coefficient accCoefficient, BitArray accQubits, BitArray accFermiSites, int row)
    {
        var coefficient = accCoefficient * Coefficients[row];
        if (accFermiSites.ExchangeParityWith(FermiRows[row]))
        {
            coefficient *= Coefficient.MinusOne;
        }

        accQubits.Xor(QubitRows[row]);
        accFermiSites.Xor(FermiRows[row]);
        accCoefficient = coefficient;
    }

    private BitArray Flatten(BitArray qubits, BitArray fermiSites)
    {
        var flattened = new BitArray(fermiSites.Length + qubits.Length);
        for (int i = 0; i < fermiSites.Length; i++)
        {
            flattened[i] = fermiSites[i];
        }

        for (int i = 0; i < qubits.Length; i++)
        {
            flattened[fermiSites.Length + i] = qubits[i];
        }

        return flattened;
    }

    private void ValidateRowIndex(int row)
    {
        if ((uint)row >= (uint)TotalRows)
        {
            throw new ArgumentOutOfRangeException(nameof(row));
        }
    }

    private void ValidateOperatorDimensions(BitArray qubits, BitArray fermiSites)
    {
        if (qubits.Length != _qubitColumns || fermiSites.Length != _fermiColumns)
        {
            throw new ArgumentException("Operator dimensions do not match platform state frame.");
        }
    }
}