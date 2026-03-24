// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Utility;

internal static class CliffordTransformUtility
{
    public static int GetPauliXColumn(int qubitIndex) => 2 * qubitIndex;

    public static int GetPauliZColumn(int qubitIndex) => (2 * qubitIndex) + 1;

    public static Coefficient ApplyH(PackedBits qubits, int qubitIndex)
    {
        int xColumn = GetPauliXColumn(qubitIndex);
        int zColumn = GetPauliZColumn(qubitIndex);
        bool xOccupied = qubits.GetBitUnchecked(xColumn);
        bool zOccupied = qubits.GetBitUnchecked(zColumn);
        qubits.SwapBitsUnchecked(xColumn, zColumn);
        return xOccupied && zOccupied ? Coefficient.MinusOne : Coefficient.PlusOne;
    }

    public static Coefficient ApplyS(PackedBits qubits, int qubitIndex)
    {
        int xColumn = GetPauliXColumn(qubitIndex);
        int zColumn = GetPauliZColumn(qubitIndex);
        bool xOccupied = qubits.GetBitUnchecked(xColumn);
        if (xOccupied)
        {
            qubits.ToggleBitUnchecked(zColumn);
        }

        return xOccupied ? Coefficient.PlusI : Coefficient.PlusOne;
    }

    public static void ApplyCX(PackedBits qubits, int controlIndex, int targetIndex)
    {
        int controlXColumn = GetPauliXColumn(controlIndex);
        int controlZColumn = GetPauliZColumn(controlIndex);
        int targetXColumn = GetPauliXColumn(targetIndex);
        int targetZColumn = GetPauliZColumn(targetIndex);

        bool controlX = qubits.GetBitUnchecked(controlXColumn);
        bool targetZ = qubits.GetBitUnchecked(targetZColumn);
        if (controlX)
        {
            qubits.ToggleBitUnchecked(targetXColumn);
        }

        if (targetZ)
        {
            qubits.ToggleBitUnchecked(controlZColumn);
        }
    }
}
