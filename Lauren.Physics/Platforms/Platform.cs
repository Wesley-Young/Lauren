using System.Collections;
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

        var occupiedZ = new BitArray(PauliCount);
        occupiedZ[qubitIndex] = true;
        var zOperator = new PauliOperator(new BitArray(PauliCount), occupiedZ, Coefficient.PlusOne);
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

        var detection = OperatorEmbeddingUtility.EmbedPauli(op, PauliCount);
        if (!_state.TrySolvePauliSpan(detection.Qubits, out bool[] solution))
        {
            return null;
        }

        var evaluatedCoefficient = _state.MultiplySelectedCoefficient(solution);
        return evaluatedCoefficient == detection.Coefficient ? 1 : -1;
    }

    public int Measure(QuantumOperator op)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Measurement operator must be Hermitian.");
        }

        var measurement = OperatorEmbeddingUtility.EmbedPauli(op, PauliCount);

        int firstAnticommutingIndex = -1;
        for (int i = 0; i < _state.TotalRows; i++)
        {
            if (_state.CommutesPauli(i, measurement.Qubits))
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
                ? measurement.Coefficient
                : measurement.Coefficient * Coefficient.MinusOne;
            _state.OverwritePauliRow(firstAnticommutingIndex, measurement.Qubits, coefficient);
            return isPlusOutcome ? 1 : -1;
        }

        if (!_state.TrySolvePauliSpan(measurement.Qubits, out bool[] solution))
        {
            throw new InvalidOperationException("Measured operator is not in the span of current stabilizers.");
        }

        var evaluatedCoefficient = _state.MultiplySelectedCoefficient(solution);
        return evaluatedCoefficient == measurement.Coefficient ? 1 : -1;
    }
}
