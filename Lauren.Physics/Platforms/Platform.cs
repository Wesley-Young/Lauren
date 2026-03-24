using Lauren.Physics.Operators;
using Lauren.Physics.Utility;

// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Platforms;

/// <summary>
///     Define a platform state evolution and measurement model based on the Pauli stabilizer representation.
/// </summary>
public class Platform
{
    private readonly PlatformStateFrame _state;

    public Platform(int pauliCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pauliCount);

        PauliCount = pauliCount;
        _state = new PlatformStateFrame(pauliCount, 2 * pauliCount, 0);

        for (int i = 0; i < pauliCount; i++)
        {
            _state.Coefficients[i] = Coefficient.PlusOne;
            _state.QubitRows[i][CliffordTransformUtility.GetPauliZColumn(i)] = true;
        }
    }

    public int PauliCount { get; }

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

    public void H(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyH(_state.QubitRows[i], qubitIndex);
        }
    }

    public void S(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        for (int i = 0; i < _state.TotalRows; i++)
        {
            _state.Coefficients[i] *= CliffordTransformUtility.ApplyS(_state.QubitRows[i], qubitIndex);
        }
    }

    public void CX(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(controlIndex, PauliCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidatePauliQubitIndex(targetIndex, PauliCount, nameof(targetIndex));

        for (int i = 0; i < _state.TotalRows; i++)
        {
            CliffordTransformUtility.ApplyCX(_state.QubitRows[i], controlIndex, targetIndex);
        }
    }

    public void XError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            X(qubitIndex);
        }
    }

    public void YError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            Y(qubitIndex);
        }
    }

    public void ZError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            Z(qubitIndex);
        }
    }

    public void Reset(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

        var occupiedX = new PackedBits(PauliCount);
        var occupiedZ = new PackedBits(PauliCount);
        occupiedZ[qubitIndex] = true;
        var zOperator = new PauliOperator(occupiedX, occupiedZ, Coefficient.PlusOne);
        if (Measure(zOperator) == -1)
        {
            X(qubitIndex);
        }
    }

    public int? Detect(QuantumOperator op)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Detection operator must be Hermitian.");
        }

        if (op is not PauliOperator pauli)
        {
            throw new ArgumentException("Detection operator must be a PauliOperator.", nameof(op));
        }

        if (pauli.OccupiedXPacked.Length != PauliCount)
        {
            throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.", nameof(op));
        }

        var detectionQubits = pauli.ZippedOccupationsPacked();
        if (!_state.TrySolvePauliSpan(detectionQubits, out bool[] solution))
        {
            return null;
        }

        var evaluatedCoefficient = _state.MultiplySelectedCoefficient(solution);
        return evaluatedCoefficient == pauli.Coefficient ? 1 : -1;
    }

    public int Measure(QuantumOperator op)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Measurement operator must be Hermitian.");
        }

        if (op is not PauliOperator pauli)
        {
            throw new ArgumentException("Measurement operator must be a PauliOperator.", nameof(op));
        }

        if (pauli.OccupiedXPacked.Length != PauliCount)
        {
            throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.", nameof(op));
        }

        var measurementQubits = pauli.ZippedOccupationsPacked();

        int firstAnticommutingIndex = -1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            if (_state.CommutesPauli(i, measurementQubits))
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

        if (firstAnticommutingIndex != -1)
        {
            bool isPlusOutcome = Random.Shared.NextDouble() < 0.5;
            var coefficient = isPlusOutcome
                ? pauli.Coefficient
                : pauli.Coefficient * Coefficient.MinusOne;
            _state.OverwritePauliRow(firstAnticommutingIndex, measurementQubits, coefficient);
            return isPlusOutcome ? 1 : -1;
        }

        if (!_state.TrySolvePauliSpan(measurementQubits, out bool[] solution))
        {
            throw new InvalidOperationException("Measured operator is not in the span of current stabilizers.");
        }

        var evaluatedCoefficient = _state.MultiplySelectedCoefficient(solution);
        return evaluatedCoefficient == pauli.Coefficient ? 1 : -1;
    }
}