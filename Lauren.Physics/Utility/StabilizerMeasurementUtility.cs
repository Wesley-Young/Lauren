using System.Collections;
using Lauren.Physics.Operators;
using Lauren.Physics.Platforms;

namespace Lauren.Physics.Utility;

public static class StabilizerMeasurementUtility
{
    public static Stabilizer BuildMeasurementStabilizer(QuantumOperator op, int pauliCount, int majoranaCount)
    {
        return op switch
        {
            PauliOperator pauli => BuildPauliMeasurementStabilizer(pauli, pauliCount, majoranaCount),
            MajoranaOperator majorana => BuildMajoranaMeasurementStabilizer(majorana, pauliCount, majoranaCount),
            _ => throw new ArgumentException("Measurement operator must be either PauliOperator or MajoranaOperator.", nameof(op))
        };
    }

    public static bool TrySolveSpan(IReadOnlyList<Stabilizer> stabilizers, Stabilizer target, out bool[] solution)
    {
        int rowCount = stabilizers.Count;
        int colCount = target.FermiSites.Length + target.Qubits.Length;

        var rows = new BitArray[rowCount];
        var combos = new BitArray[rowCount];
        for (int i = 0; i < rowCount; i++)
        {
            if (stabilizers[i].Qubits.Length != target.Qubits.Length ||
                stabilizers[i].FermiSites.Length != target.FermiSites.Length)
            {
                throw new ArgumentException("Stabilizers must have matching qubit and fermi-site dimensions.");
            }

            rows[i] = stabilizers[i].Flatten();
            combos[i] = new BitArray(rowCount);
            combos[i][i] = true;
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

        var reducedTarget = target.Flatten();
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

    public static Stabilizer MultiplySelected(IReadOnlyList<Stabilizer> stabilizers, IReadOnlyList<bool> selector)
    {
        if (stabilizers.Count == 0)
        {
            throw new ArgumentException("At least one stabilizer is required.", nameof(stabilizers));
        }

        if (selector.Count != stabilizers.Count)
        {
            throw new ArgumentException("Selector length must match stabilizer count.", nameof(selector));
        }

        var acc = new Stabilizer
        {
            Coefficient = Coefficient.PlusOne,
            Qubits = new BitArray(stabilizers[0].Qubits.Length),
            FermiSites = new BitArray(stabilizers[0].FermiSites.Length)
        };

        for (int i = 0; i < stabilizers.Count; i++)
        {
            if (!selector[i])
            {
                continue;
            }

            acc.MultiplyInPlace(stabilizers[i]);
        }

        return acc;
    }

    private static Stabilizer BuildPauliMeasurementStabilizer(PauliOperator op, int pauliCount, int majoranaCount)
    {
        if (op.OccupiedX.Length != pauliCount)
        {
            throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.");
        }

        return new Stabilizer
        {
            Coefficient = op.Coefficient,
            Qubits = op.ZippedOccupations(),
            FermiSites = new BitArray(2 * majoranaCount)
        };
    }

    private static Stabilizer BuildMajoranaMeasurementStabilizer(MajoranaOperator op, int pauliCount, int majoranaCount)
    {
        if (op.OccupiedX.Length != majoranaCount)
        {
            throw new ArgumentException("Majorana operator size does not match platform Majorana site count.");
        }

        return new Stabilizer
        {
            Coefficient = op.Coefficient,
            Qubits = new BitArray(2 * pauliCount),
            FermiSites = op.ZippedOccupations()
        };
    }

}
