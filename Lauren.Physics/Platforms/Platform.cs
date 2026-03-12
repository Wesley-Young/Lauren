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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

        int zColumn = CliffordTransformUtility.GetPauliZColumn(qubitIndex);
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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

        int xColumn = CliffordTransformUtility.GetPauliXColumn(qubitIndex);
        int zColumn = CliffordTransformUtility.GetPauliZColumn(qubitIndex);
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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

        int xColumn = CliffordTransformUtility.GetPauliXColumn(qubitIndex);
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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyH(_state.QubitRows[i], qubitIndex);
        }

    }

    /// <summary>
    ///     Apply S phase gate on a Pauli qubit.
    /// </summary>
    public void S(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyS(_state.QubitRows[i], qubitIndex);
        }

    }

    /// <summary>
    ///     Apply gamma gate on a Majorana qubit.
    /// </summary>
    public void U(int majoranaIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);

        int xColumn = CliffordTransformUtility.GetMajoranaXColumn(majoranaIndex);
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
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);

        int zColumn = CliffordTransformUtility.GetMajoranaZColumn(majoranaIndex);
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
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);

        int xColumn = CliffordTransformUtility.GetMajoranaXColumn(majoranaIndex);
        int zColumn = CliffordTransformUtility.GetMajoranaZColumn(majoranaIndex);
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
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyP(_state.FermiRows[i], majoranaIndex);
        }
    }

    /// <summary>
    ///     Apply controlled-X gate on Pauli qubits.
    /// </summary>
    public void CX(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(controlIndex, PauliCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidatePauliQubitIndex(targetIndex, PauliCount, nameof(targetIndex));

        for (int i = 0; i < _state.TotalRows; i++)
        {
            CliffordTransformUtility.ApplyCX(_state.QubitRows[i], controlIndex, targetIndex);
        }
    }

    /// <summary>
    ///     Apply controlled-X gate from a Majorana qubit onto a Pauli qubit.
    /// </summary>
    public void CNX(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(controlIndex, MajoranaCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidatePauliQubitIndex(targetIndex, PauliCount, nameof(targetIndex));

        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyCNX(
                _state.FermiRows[i],
                _state.QubitRows[i],
                controlIndex,
                targetIndex);
        }
    }

    /// <summary>
    ///     Apply controlled-N gate between two Majorana qubits.
    /// </summary>
    public void CNN(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(controlIndex, MajoranaCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(targetIndex, MajoranaCount, nameof(targetIndex));

        if (targetIndex < controlIndex)
        {
            (controlIndex, targetIndex) = (targetIndex, controlIndex);
        }

        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyCNN(_state.FermiRows[i], controlIndex, targetIndex);
        }
    }

    /// <summary>
    ///     Apply braid gate between two Majorana qubits.
    /// </summary>
    public void Braid(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(controlIndex, MajoranaCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(targetIndex, MajoranaCount, nameof(targetIndex));

        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyBraid(_state.FermiRows[i], controlIndex, targetIndex);
        }
    }

    /// <summary>
    ///     Apply Pauli-X noise on a Pauli qubit with probability <paramref name="probability" />.
    /// </summary>
    public void XError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
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
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        PlatformArgumentUtility.ValidateProbability(probability);
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
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        PlatformArgumentUtility.ValidateProbability(probability);
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
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        PlatformArgumentUtility.ValidateProbability(probability);
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
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

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

        var detection = OperatorEmbeddingUtility.Embed(op, PauliCount, MajoranaCount);
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

        var measurement = OperatorEmbeddingUtility.Embed(op, PauliCount, MajoranaCount);

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

}
