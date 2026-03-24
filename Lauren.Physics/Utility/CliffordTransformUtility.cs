using System.Collections;
// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Utility;

internal static class CliffordTransformUtility
{
    public static int GetPauliXColumn(int qubitIndex) => 2 * qubitIndex;

    public static int GetPauliZColumn(int qubitIndex) => (2 * qubitIndex) + 1;

    public static Coefficient ApplyH(BitArray qubits, int qubitIndex)
    {
        int xColumn = GetPauliXColumn(qubitIndex);
        int zColumn = GetPauliZColumn(qubitIndex);
        bool xOccupied = qubits[xColumn];
        bool zOccupied = qubits[zColumn];
        qubits[xColumn] = zOccupied;
        qubits[zColumn] = xOccupied;
        return xOccupied && zOccupied ? Coefficient.MinusOne : Coefficient.PlusOne;
    }

    public static Coefficient ApplyS(BitArray qubits, int qubitIndex)
    {
        int xColumn = GetPauliXColumn(qubitIndex);
        int zColumn = GetPauliZColumn(qubitIndex);
        bool xOccupied = qubits[xColumn];
        qubits[zColumn] ^= xOccupied;
        return xOccupied ? Coefficient.PlusI : Coefficient.PlusOne;
    }

    public static void ApplyCX(BitArray qubits, int controlIndex, int targetIndex)
    {
        int controlXColumn = GetPauliXColumn(controlIndex);
        int controlZColumn = GetPauliZColumn(controlIndex);
        int targetXColumn = GetPauliXColumn(targetIndex);
        int targetZColumn = GetPauliZColumn(targetIndex);

        bool controlX = qubits[controlXColumn];
        bool targetZ = qubits[targetZColumn];
        qubits[targetXColumn] ^= controlX;
        qubits[controlZColumn] ^= targetZ;
    }
}
