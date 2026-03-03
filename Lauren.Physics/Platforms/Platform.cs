using System.Collections;
using Lauren.Physics.Operators;

namespace Lauren.Physics.Platforms;

/// <summary>
///     Define a platform state evolution and measurement model based on the stabilizer representation,
///     supporting Pauli and Majorana gates, noise, measurement, and reset.
/// </summary>
public class Platform
{
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

        (Coefficient Coefficient, BitArray Qubits, BitArray FermiSites) measurement;
        switch (op)
        {
            case PauliOperator pauli:
                if (pauli.OccupiedX.Length != PauliCount)
                {
                    throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.");
                }

                measurement = (
                    pauli.Coefficient,
                    pauli.ZippedOccupations(),
                    new BitArray(2 * MajoranaCount));
                break;

            case MajoranaOperator majorana:
                if (majorana.OccupiedX.Length != MajoranaCount)
                {
                    throw new ArgumentException("Majorana operator size does not match platform Majorana site count.");
                }

                measurement = (
                    majorana.Coefficient,
                    new BitArray(2 * PauliCount),
                    majorana.ZippedOccupations());
                break;

            default:
                throw new ArgumentException(
                    "Measurement operator must be either PauliOperator or MajoranaOperator.",
                    nameof(op));
        }

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
}