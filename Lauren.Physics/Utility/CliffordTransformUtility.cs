using System.Collections;
// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Utility;

internal static class CliffordTransformUtility
{
    private static readonly Coefficient[] CnnPhaseByPattern =
    [
        Coefficient.PlusOne, Coefficient.PlusI, Coefficient.PlusI, Coefficient.PlusOne,
        Coefficient.PlusI, Coefficient.PlusOne, Coefficient.MinusOne, Coefficient.MinusI,
        Coefficient.PlusI, Coefficient.MinusOne, Coefficient.PlusOne, Coefficient.MinusI,
        Coefficient.PlusOne, Coefficient.MinusI, Coefficient.MinusI, Coefficient.PlusOne
    ];

    public static int GetPauliXColumn(int qubitIndex) => 2 * qubitIndex;

    public static int GetPauliZColumn(int qubitIndex) => (2 * qubitIndex) + 1;

    public static int GetMajoranaXColumn(int majoranaIndex) => 2 * majoranaIndex;

    public static int GetMajoranaZColumn(int majoranaIndex) => (2 * majoranaIndex) + 1;

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

    public static Coefficient ApplyP(BitArray fermiSites, int majoranaIndex)
    {
        int xColumn = GetMajoranaXColumn(majoranaIndex);
        int zColumn = GetMajoranaZColumn(majoranaIndex);
        bool xOccupied = fermiSites[xColumn];
        bool zOccupied = fermiSites[zColumn];
        fermiSites[xColumn] = zOccupied;
        fermiSites[zColumn] = xOccupied;
        return !xOccupied && zOccupied ? Coefficient.MinusOne : Coefficient.PlusOne;
    }

    public static Coefficient ApplyCX(BitArray qubits, int controlIndex, int targetIndex)
    {
        int controlXColumn = GetPauliXColumn(controlIndex);
        int controlZColumn = GetPauliZColumn(controlIndex);
        int targetXColumn = GetPauliXColumn(targetIndex);
        int targetZColumn = GetPauliZColumn(targetIndex);

        bool controlX = qubits[controlXColumn];
        bool targetZ = qubits[targetZColumn];
        qubits[targetXColumn] ^= controlX;
        qubits[controlZColumn] ^= targetZ;
        return Coefficient.PlusOne;
    }

    public static Coefficient ApplyCNX(BitArray fermiSites, BitArray qubits, int controlIndex, int targetIndex)
    {
        int controlXColumn = GetMajoranaXColumn(controlIndex);
        int controlZColumn = GetMajoranaZColumn(controlIndex);
        int targetXColumn = GetPauliXColumn(targetIndex);
        int targetZColumn = GetPauliZColumn(targetIndex);

        bool controlX = fermiSites[controlXColumn];
        bool controlZ = fermiSites[controlZColumn];
        bool targetZ = qubits[targetZColumn];

        qubits[targetXColumn] ^= controlX ^ controlZ;
        fermiSites[controlXColumn] = controlX ^ targetZ;
        fermiSites[controlZColumn] = controlZ ^ targetZ;

        if (!targetZ)
        {
            return Coefficient.PlusOne;
        }

        return controlZ ? Coefficient.MinusI : Coefficient.PlusI;
    }

    public static Coefficient ApplyCNN(BitArray fermiSites, int controlIndex, int targetIndex)
    {
        int controlXColumn = GetMajoranaXColumn(controlIndex);
        int controlZColumn = GetMajoranaZColumn(controlIndex);
        int targetXColumn = GetMajoranaXColumn(targetIndex);
        int targetZColumn = GetMajoranaZColumn(targetIndex);

        bool controlX = fermiSites[controlXColumn];
        bool controlZ = fermiSites[controlZColumn];
        bool targetX = fermiSites[targetXColumn];
        bool targetZ = fermiSites[targetZColumn];

        int pattern =
            (controlX ? 0b1000 : 0) |
            (controlZ ? 0b0100 : 0) |
            (targetX ? 0b0010 : 0) |
            (targetZ ? 0b0001 : 0);

        fermiSites[controlXColumn] = controlX ^ targetX ^ targetZ;
        fermiSites[controlZColumn] = controlZ ^ targetX ^ targetZ;
        fermiSites[targetXColumn] = targetX ^ controlX ^ controlZ;
        fermiSites[targetZColumn] = targetZ ^ controlX ^ controlZ;
        return CnnPhaseByPattern[pattern];
    }

    public static Coefficient ApplyBraid(BitArray fermiSites, int controlIndex, int targetIndex)
    {
        int controlZColumn = GetMajoranaZColumn(controlIndex);
        int targetXColumn = GetMajoranaXColumn(targetIndex);

        int middleStart = Math.Min(controlZColumn, targetXColumn) + 1;
        int middleEnd = Math.Max(controlZColumn, targetXColumn);

        bool controlZ = fermiSites[controlZColumn];
        bool targetX = fermiSites[targetXColumn];
        bool middleParityOdd = RangeParity(fermiSites, middleStart, middleEnd);

        bool phaseOdd = false;
        if (controlZ && middleParityOdd) phaseOdd = !phaseOdd;
        if (targetX && middleParityOdd) phaseOdd = !phaseOdd;
        if (controlZ && targetX) phaseOdd = !phaseOdd;
        if (targetX) phaseOdd = !phaseOdd;

        fermiSites[controlZColumn] = targetX;
        fermiSites[targetXColumn] = controlZ;
        return phaseOdd ? Coefficient.MinusOne : Coefficient.PlusOne;
    }

    private static bool RangeParity(BitArray bits, int startInclusive, int endExclusive)
    {
        bool parity = false;
        for (int i = startInclusive; i < endExclusive; i++)
        {
            if (bits[i])
            {
                parity = !parity;
            }
        }

        return parity;
    }
}
