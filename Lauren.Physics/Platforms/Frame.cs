using Lauren.Physics.Operators;
using Lauren.Physics.Utility;

// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Platforms;

/// <summary>
///     Tracks a simplified Pauli frame relative to a reference trajectory.
///     This model intentionally includes random gauge/branch bits introduced by initialization,
///     reset, and anticommuting measurements, matching the behavior used by ExtendedStim's
///     prototype sampling path.
/// </summary>
public sealed class Frame : ICliffordPlatform
{
    private PackedBits _qubitFrame = new(0);

    public int PauliCount { get; private set; }

    /// <summary>
    ///     Initializes the frame for the given number of qubits and randomly seeds per-qubit Z gauge bits.
    ///     The initial frame is therefore not required to be the identity frame.
    /// </summary>
    public void Trap(int pauliCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pauliCount);

        PauliCount = pauliCount;
        _qubitFrame = new PackedBits(2 * pauliCount);

        for (int i = 0; i < pauliCount; i++)
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                _qubitFrame.SetBitUnchecked(CliffordTransformUtility.GetPauliZColumn(i), true);
            }
        }
    }

    public int Measure(QuantumOperator op, int referenceValue)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Measurement operator must be Hermitian.");
        }

        PlatformArgumentUtility.ValidateReferenceMeasurementValue(referenceValue);
        if (op is not PauliOperator pauli)
        {
            throw new ArgumentException("Measurement operator must be a PauliOperator.", nameof(op));
        }

        if (pauli.OccupiedXPacked.Length != PauliCount)
        {
            throw new ArgumentException("Pauli operator size does not match platform Pauli qubit count.", nameof(op));
        }

        return MeasurePauli(pauli.ZippedOccupationsPacked(), referenceValue);
    }

    internal int MeasurePauli(PackedBits measurementQubits, int referenceValue)
    {
        bool commutes = CommutationUtility.CommutesPauli(_qubitFrame, measurementQubits);
        int result = commutes ? referenceValue : -referenceValue;

        if (Random.Shared.NextDouble() < 0.5)
        {
            _qubitFrame.XorInPlace(measurementQubits);
        }

        return result;
    }

    public void X(int qubitIndex) => PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

    public void Y(int qubitIndex) => PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

    public void Z(int qubitIndex) => PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

    public void CX(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(controlIndex, PauliCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidatePauliQubitIndex(targetIndex, PauliCount, nameof(targetIndex));
        CliffordTransformUtility.ApplyCX(_qubitFrame, controlIndex, targetIndex);
    }

    public void H(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        CliffordTransformUtility.ApplyH(_qubitFrame, qubitIndex);
    }

    public void S(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        CliffordTransformUtility.ApplyS(_qubitFrame, qubitIndex);
    }

    public void XError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            _qubitFrame.ToggleBitUnchecked(CliffordTransformUtility.GetPauliXColumn(qubitIndex));
        }
    }

    public void YError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            int xColumn = CliffordTransformUtility.GetPauliXColumn(qubitIndex);
            int zColumn = CliffordTransformUtility.GetPauliZColumn(qubitIndex);
            _qubitFrame.ToggleBitUnchecked(xColumn);
            _qubitFrame.ToggleBitUnchecked(zColumn);
        }
    }

    public void ZError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            _qubitFrame.ToggleBitUnchecked(CliffordTransformUtility.GetPauliZColumn(qubitIndex));
        }
    }

    public void Reset(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        int xColumn = CliffordTransformUtility.GetPauliXColumn(qubitIndex);
        int zColumn = CliffordTransformUtility.GetPauliZColumn(qubitIndex);
        _qubitFrame.SetBitUnchecked(xColumn, false);
        _qubitFrame.SetBitUnchecked(zColumn, false);

        // Reset clears the tracked frame on the qubit, then reintroduces a random Z gauge bit.
        if (Random.Shared.NextDouble() < 0.5)
        {
            _qubitFrame.SetBitUnchecked(zColumn, true);
        }
    }
}
