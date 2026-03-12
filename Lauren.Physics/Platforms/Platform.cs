using System.Collections;
using Lauren.Physics.Operators;
using Lauren.Physics.Utility;
// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Platforms;

/// <summary>
///     Define a platform state evolution and measurement model based on the stabilizer representation,
///     supporting Pauli and Majorana gates, noise, measurement, and reset.
/// </summary>
public class Platform
{
    private static readonly Coefficient[] CnnPhaseByPattern =
    [
        Coefficient.PlusOne, Coefficient.PlusI, Coefficient.PlusI, Coefficient.PlusOne,
        Coefficient.PlusI, Coefficient.PlusOne, Coefficient.MinusOne, Coefficient.MinusI,
        Coefficient.PlusI, Coefficient.MinusOne, Coefficient.PlusOne, Coefficient.MinusI,
        Coefficient.PlusOne, Coefficient.MinusI, Coefficient.MinusI, Coefficient.PlusOne
    ];

    private readonly PlatformStateFrame _state;

    /// <summary>
    ///     Construct a platform with given numbers of Pauli qubits and Majorana fermi sites trapped.
    /// </summary>
    /// <param name="pauliCount">The number of Pauli qubits to trap.</param>
    /// <param name="majoranaCount">The number of Majorana fermi sites to trap.</param>
    public Platform(int pauliCount, int majoranaCount)
    {
        if (pauliCount < 0) throw new ArgumentOutOfRangeException(nameof(pauliCount));
        if (majoranaCount < 0) throw new ArgumentOutOfRangeException(nameof(majoranaCount));

        PauliCount = pauliCount;
        MajoranaCount = majoranaCount;

        _state = new PlatformStateFrame(
            pauliCount + majoranaCount,
            2 * pauliCount,
            2 * majoranaCount);

        for (int i = 0; i < pauliCount; i++)
        {
            _state.Coefficients[i] = Coefficient.PlusOne;
            _state.QubitRows[i][(2 * i) + 1] = true;
        }

        for (int i = 0; i < majoranaCount; i++)
        {
            int row = pauliCount + i;
            _state.Coefficients[row] = Coefficient.PlusI;
            _state.FermiRows[row][2 * i] = true;
            _state.FermiRows[row][(2 * i) + 1] = true;
        }
    }

    /// <summary>
    ///     Count of Pauli qubits in the platform.
    /// </summary>
    public int PauliCount { get; }

    /// <summary>
    ///     Count of Fermi sites in the platform.
    /// </summary>
    public int MajoranaCount { get; }

    /// <summary>
    ///     Apply Pauli-X gate on a Pauli qubit.
    /// </summary>
    public void X(int qubitIndex)
    {
        ValidatePauliQubitIndex(qubitIndex);

        int zColumn = (2 * qubitIndex) + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            if (_state.QubitRows[i][zColumn])
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
        }

    }

    /// <summary>
    ///     Apply Pauli-Y gate on a Pauli qubit.
    /// </summary>
    public void Y(int qubitIndex)
    {
        ValidatePauliQubitIndex(qubitIndex);

        int xColumn = 2 * qubitIndex;
        int zColumn = xColumn + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            if (_state.QubitRows[i][zColumn])
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
            if (_state.QubitRows[i][xColumn])
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
        }

    }

    /// <summary>
    ///     Apply Pauli-Z gate on a Pauli qubit.
    /// </summary>
    public void Z(int qubitIndex)
    {
        ValidatePauliQubitIndex(qubitIndex);

        int xColumn = 2 * qubitIndex;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            if (_state.QubitRows[i][xColumn])
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
        }

    }

    /// <summary>
    ///     Apply Hadamard gate on a Pauli qubit.
    /// </summary>
    public void H(int qubitIndex)
    {
        ValidatePauliQubitIndex(qubitIndex);

        int xColumn = 2 * qubitIndex;
        int zColumn = xColumn + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool xOccupied = _state.QubitRows[i][xColumn];
            bool zOccupied = _state.QubitRows[i][zColumn];
            if (xOccupied && zOccupied)
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }

            _state.QubitRows[i][xColumn] = zOccupied;
            _state.QubitRows[i][zColumn] = xOccupied;
        }

    }

    /// <summary>
    ///     Apply S phase gate on a Pauli qubit.
    /// </summary>
    public void S(int qubitIndex)
    {
        ValidatePauliQubitIndex(qubitIndex);

        int xColumn = 2 * qubitIndex;
        int zColumn = xColumn + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool xOccupied = _state.QubitRows[i][xColumn];
            if (xOccupied)
            {
                _state.Coefficients[i] *= Coefficient.PlusI;
            }

            _state.QubitRows[i][zColumn] ^= xOccupied;
        }

    }

    /// <summary>
    ///     Apply gamma gate on a Majorana qubit.
    /// </summary>
    public void U(int majoranaIndex)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);

        int xColumn = 2 * majoranaIndex;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool oddWeight = (_state.FermiRows[i].Weight() & 1) != 0;
            bool overlaps = _state.FermiRows[i][xColumn];
            if (oddWeight == overlaps)
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
        }
    }

    /// <summary>
    ///     Apply gamma-prime gate on a Majorana qubit.
    /// </summary>
    public void V(int majoranaIndex)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);

        int zColumn = (2 * majoranaIndex) + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool oddWeight = (_state.FermiRows[i].Weight() & 1) != 0;
            bool overlaps = _state.FermiRows[i][zColumn];
            if (oddWeight == overlaps)
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
        }
    }

    /// <summary>
    ///     Apply i*gamma*gamma-prime gate on a Majorana qubit.
    /// </summary>
    public void N(int majoranaIndex)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);

        int xColumn = 2 * majoranaIndex;
        int zColumn = xColumn + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool oddWeight = (_state.FermiRows[i].Weight() & 1) != 0;
            bool overlaps = _state.FermiRows[i][xColumn] ^ _state.FermiRows[i][zColumn];
            if (oddWeight != overlaps)
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }
        }
    }

    /// <summary>
    ///     Apply P gate on a Majorana qubit.
    /// </summary>
    public void P(int majoranaIndex)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);

        int xColumn = 2 * majoranaIndex;
        int zColumn = xColumn + 1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool xOccupied = _state.FermiRows[i][xColumn];
            bool zOccupied = _state.FermiRows[i][zColumn];
            if (!xOccupied && zOccupied)
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }

            _state.FermiRows[i][xColumn] = zOccupied;
            _state.FermiRows[i][zColumn] = xOccupied;
        }
    }

    /// <summary>
    ///     Apply controlled-X gate on Pauli qubits.
    /// </summary>
    public void CX(int controlIndex, int targetIndex)
    {
        ValidatePauliQubitIndex(controlIndex);
        ValidatePauliQubitIndex(targetIndex);

        int controlXColumn = 2 * controlIndex;
        int controlZColumn = controlXColumn + 1;
        int targetXColumn = 2 * targetIndex;
        int targetZColumn = targetXColumn + 1;

        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool controlX = _state.QubitRows[i][controlXColumn];
            bool targetZ = _state.QubitRows[i][targetZColumn];

            _state.QubitRows[i][targetXColumn] ^= controlX;
            _state.QubitRows[i][controlZColumn] ^= targetZ;
        }
    }

    /// <summary>
    ///     Apply controlled-X gate from a Majorana qubit onto a Pauli qubit.
    /// </summary>
    public void CNX(int controlIndex, int targetIndex)
    {
        ValidateMajoranaQubitIndex(controlIndex);
        ValidatePauliQubitIndex(targetIndex);

        int controlXColumn = 2 * controlIndex;
        int controlZColumn = controlXColumn + 1;
        int targetXColumn = 2 * targetIndex;
        int targetZColumn = targetXColumn + 1;

        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool controlX = _state.FermiRows[i][controlXColumn];
            bool controlZ = _state.FermiRows[i][controlZColumn];
            bool targetZ = _state.QubitRows[i][targetZColumn];
            if (targetZ)
            {
                _state.Coefficients[i] *= Coefficient.PlusI;
                if (controlZ)
                {
                    _state.Coefficients[i] *= Coefficient.MinusOne;
                }
            }

            _state.QubitRows[i][targetXColumn] ^= controlX ^ controlZ;
            _state.FermiRows[i][controlXColumn] = controlX ^ targetZ;
            _state.FermiRows[i][controlZColumn] = controlZ ^ targetZ;
        }
    }

    /// <summary>
    ///     Apply controlled-N gate between two Majorana qubits.
    /// </summary>
    public void CNN(int controlIndex, int targetIndex)
    {
        ValidateMajoranaQubitIndex(controlIndex);
        ValidateMajoranaQubitIndex(targetIndex);

        if (targetIndex < controlIndex)
        {
            (controlIndex, targetIndex) = (targetIndex, controlIndex);
        }

        int controlXColumn = 2 * controlIndex;
        int controlZColumn = controlXColumn + 1;
        int targetXColumn = 2 * targetIndex;
        int targetZColumn = targetXColumn + 1;

        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool controlX = _state.FermiRows[i][controlXColumn];
            bool controlZ = _state.FermiRows[i][controlZColumn];
            bool targetX = _state.FermiRows[i][targetXColumn];
            bool targetZ = _state.FermiRows[i][targetZColumn];

            int pattern =
                (controlX ? 0b1000 : 0) |
                (controlZ ? 0b0100 : 0) |
                (targetX ? 0b0010 : 0) |
                (targetZ ? 0b0001 : 0);
            _state.Coefficients[i] *= CnnPhaseByPattern[pattern];

            _state.FermiRows[i][controlXColumn] = controlX ^ targetX ^ targetZ;
            _state.FermiRows[i][controlZColumn] = controlZ ^ targetX ^ targetZ;
            _state.FermiRows[i][targetXColumn] = targetX ^ controlX ^ controlZ;
            _state.FermiRows[i][targetZColumn] = targetZ ^ controlX ^ controlZ;
        }
    }

    /// <summary>
    ///     Apply braid gate between two Majorana qubits.
    /// </summary>
    public void Braid(int controlIndex, int targetIndex)
    {
        ValidateMajoranaQubitIndex(controlIndex);
        ValidateMajoranaQubitIndex(targetIndex);

        int controlZColumn = (2 * controlIndex) + 1;
        int targetXColumn = 2 * targetIndex;

        int middleStart = Math.Min(controlZColumn, targetXColumn) + 1;
        int middleEnd = Math.Max(controlZColumn, targetXColumn);

        for (int i = 0; i < _state.TotalRows; i++)
        {
            bool controlZ = _state.FermiRows[i][controlZColumn];
            bool targetX = _state.FermiRows[i][targetXColumn];
            bool middleParityOdd = RangeParity(_state.FermiRows[i], middleStart, middleEnd);

            bool phaseOdd = false;
            if (controlZ && middleParityOdd) phaseOdd = !phaseOdd;
            if (targetX && middleParityOdd) phaseOdd = !phaseOdd;
            if (controlZ && targetX) phaseOdd = !phaseOdd;
            if (targetX) phaseOdd = !phaseOdd;

            if (phaseOdd)
            {
                _state.Coefficients[i] *= Coefficient.MinusOne;
            }

            _state.FermiRows[i][controlZColumn] = targetX;
            _state.FermiRows[i][targetXColumn] = controlZ;
        }
    }

    /// <summary>
    ///     Apply Pauli-X noise on a Pauli qubit with probability <paramref name="probability" />.
    /// </summary>
    public void XError(int qubitIndex, double probability)
    {
        ValidatePauliQubitIndex(qubitIndex);
        ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            X(qubitIndex);
        }
    }

    /// <summary>
    ///     Apply Pauli-Y noise on a Pauli qubit with probability <paramref name="probability" />.
    /// </summary>
    public void YError(int qubitIndex, double probability)
    {
        ValidatePauliQubitIndex(qubitIndex);
        ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            Y(qubitIndex);
        }
    }

    /// <summary>
    ///     Apply Pauli-Z noise on a Pauli qubit with probability <paramref name="probability" />.
    /// </summary>
    public void ZError(int qubitIndex, double probability)
    {
        ValidatePauliQubitIndex(qubitIndex);
        ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            Z(qubitIndex);
        }
    }

    /// <summary>
    ///     Apply Majorana-U noise on a Majorana qubit with probability <paramref name="probability" />.
    /// </summary>
    public void UError(int majoranaIndex, double probability)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);
        ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            U(majoranaIndex);
        }
    }

    /// <summary>
    ///     Apply Majorana-V noise on a Majorana qubit with probability <paramref name="probability" />.
    /// </summary>
    public void VError(int majoranaIndex, double probability)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);
        ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            V(majoranaIndex);
        }
    }

    /// <summary>
    ///     Apply Majorana-N noise on a Majorana qubit with probability <paramref name="probability" />.
    /// </summary>
    public void NError(int majoranaIndex, double probability)
    {
        ValidateMajoranaQubitIndex(majoranaIndex);
        ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            N(majoranaIndex);
        }
    }

    /// <summary>
    ///     Reset a Pauli qubit into the +1 eigenspace of Z.
    /// </summary>
    public void Reset(int qubitIndex)
    {
        ValidatePauliQubitIndex(qubitIndex);

        var occupiedZ = new BitArray(PauliCount);
        occupiedZ[qubitIndex] = true;
        var zOperator = new PauliOperator(new BitArray(PauliCount), occupiedZ, Coefficient.PlusOne);
        if (Measure(zOperator) == -1)
        {
            X(qubitIndex);
        }
    }

    /// <summary>
    ///     Detect whether the current state lies in the eigenspace of a Hermitian operator.
    /// </summary>
    public int? Detect(QuantumOperator op)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Detection operator must be Hermitian.");
        }

        var detection = BuildMeasurement(op);
        if (!_state.TrySolveSpan(detection.Qubits, detection.FermiSites, out bool[] solution))
        {
            return null;
        }

        var evaluatedCoefficient = _state.MultiplySelectedCoefficient(solution);
        return evaluatedCoefficient == detection.Coefficient ? 1 : -1;
    }

    /// <summary>
    ///     Measure a Hermitian operator on the platform.
    /// </summary>
    /// <param name="op">
    ///     The operator to measure, which must be Hermitian and supported by the platform's site counts.
    /// </param>
    /// <returns>+1 or -1, the outcome of the measurement.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the operator is not Hermitian or does not match the platform's qubit and
    ///     fermi site counts.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the operator is not in the span of the current stabilizers after
    ///     accounting for anti-commutation.
    /// </exception>
    public int Measure(QuantumOperator op)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Measurement operator must be Hermitian.");
        }

        var measurement = BuildMeasurement(op);

        int firstAnticommutingIndex = -1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            if (_state.Commutes(i, measurement.Qubits, measurement.FermiSites))
            {
                continue;
            }

            if (firstAnticommutingIndex == -1)
            {
                firstAnticommutingIndex = i;
                continue;
            }

            _state.MultiplyRowInPlace(i, firstAnticommutingIndex);
        }

        // Yield a random outcome if there is an anti-commuting stabilizer, and collapse the state accordingly.
        if (firstAnticommutingIndex != -1)
        {
            bool isPlusOutcome = Random.Shared.NextDouble() < 0.5;
            var coefficient = isPlusOutcome
                ? measurement.Coefficient
                : measurement.Coefficient * Coefficient.MinusOne;
            _state.OverwriteRow(
                firstAnticommutingIndex,
                measurement.Qubits,
                measurement.FermiSites,
                coefficient);
            return isPlusOutcome ? 1 : -1;
        }

        // If all stabilizers commute with the measurement, the outcome is deterministic, and we can solve for it.
        if (!_state.TrySolveSpan(measurement.Qubits, measurement.FermiSites, out bool[] solution))
        {
            throw new InvalidOperationException("Measured operator is not in the span of current stabilizers.");
        }

        var evaluatedCoefficient = _state.MultiplySelectedCoefficient(solution);
        return evaluatedCoefficient == measurement.Coefficient ? 1 : -1;
    }

    private void ValidatePauliQubitIndex(int qubitIndex)
    {
        if (qubitIndex < 0 || qubitIndex >= PauliCount)
        {
            throw new ArgumentOutOfRangeException(nameof(qubitIndex), "Qubit index is out of range.");
        }
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

    private (Coefficient Coefficient, BitArray Qubits, BitArray FermiSites) BuildMeasurement(QuantumOperator op)
    {
        return op switch
        {
            PauliOperator pauli when pauli.OccupiedX.Length == PauliCount => (
                pauli.Coefficient,
                pauli.ZippedOccupations(),
                new BitArray(2 * MajoranaCount)),
            PauliOperator => throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count."),
            MajoranaOperator majorana when majorana.OccupiedX.Length == MajoranaCount => (
                majorana.Coefficient,
                new BitArray(2 * PauliCount),
                majorana.ZippedOccupations()),
            MajoranaOperator => throw new ArgumentException("Majorana operator size does not match platform Majorana site count."),
            _ => throw new ArgumentException(
                "Measurement operator must be either PauliOperator or MajoranaOperator.",
                nameof(op))
        };
    }

    private void ValidateMajoranaQubitIndex(int majoranaIndex)
    {
        if (majoranaIndex < 0 || majoranaIndex >= MajoranaCount)
        {
            throw new ArgumentOutOfRangeException(nameof(majoranaIndex), "Majorana index is out of range.");
        }
    }

    private static void ValidateProbability(double probability)
    {
        if (double.IsNaN(probability) || probability < 0d || probability > 1d)
        {
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 1.");
        }
    }
}
