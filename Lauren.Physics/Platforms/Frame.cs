using System.Collections;
using Lauren.Physics.Operators;
using Lauren.Physics.Utility;
// ReSharper disable InconsistentNaming

namespace Lauren.Physics.Platforms;

/// <summary>
///     Tracks only the relative Pauli/Majorana error frame of a reference trajectory.
/// </summary>
public sealed class Frame
{
    private BitArray _fermiFrame = new(0);
    private BitArray _qubitFrame = new(0);

    public int PauliCount { get; private set; }

    public int MajoranaCount { get; private set; }

    public void Trap(int majoranaCount, int pauliCount)
    {
        if (majoranaCount < 0) throw new ArgumentOutOfRangeException(nameof(majoranaCount));
        if (pauliCount < 0) throw new ArgumentOutOfRangeException(nameof(pauliCount));

        MajoranaCount = majoranaCount;
        PauliCount = pauliCount;
        _fermiFrame = new BitArray(2 * majoranaCount);
        _qubitFrame = new BitArray(2 * pauliCount);

        for (int i = 0; i < majoranaCount; i++)
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                _fermiFrame[CliffordTransformUtility.GetMajoranaXColumn(i)] = true;
                _fermiFrame[CliffordTransformUtility.GetMajoranaZColumn(i)] = true;
            }
        }

        for (int i = 0; i < pauliCount; i++)
        {
            if (Random.Shared.NextDouble() < 0.5)
            {
                _qubitFrame[CliffordTransformUtility.GetPauliZColumn(i)] = true;
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
        var embedded = OperatorEmbeddingUtility.Embed(op, PauliCount, MajoranaCount);
        bool commutes = CommutationUtility.Commutes(_qubitFrame, _fermiFrame, embedded.Qubits, embedded.FermiSites);
        int result = commutes ? referenceValue : -referenceValue;

        if (Random.Shared.NextDouble() < 0.5)
        {
            _qubitFrame.Xor(embedded.Qubits);
            _fermiFrame.Xor(embedded.FermiSites);
        }

        return result;
    }

    public void X(int qubitIndex) => PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

    public void Y(int qubitIndex) => PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

    public void Z(int qubitIndex) => PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);

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

    public void U(int majoranaIndex) => PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);

    public void V(int majoranaIndex) => PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);

    public void N(int majoranaIndex) => PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);

    public void P(int majoranaIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        CliffordTransformUtility.ApplyP(_fermiFrame, majoranaIndex);
    }

    public void CX(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(controlIndex, PauliCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidatePauliQubitIndex(targetIndex, PauliCount, nameof(targetIndex));
        CliffordTransformUtility.ApplyCX(_qubitFrame, controlIndex, targetIndex);
    }

    public void CNX(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(controlIndex, MajoranaCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidatePauliQubitIndex(targetIndex, PauliCount, nameof(targetIndex));
        CliffordTransformUtility.ApplyCNX(_fermiFrame, _qubitFrame, controlIndex, targetIndex);
    }

    public void CNN(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(controlIndex, MajoranaCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(targetIndex, MajoranaCount, nameof(targetIndex));
        CliffordTransformUtility.ApplyCNN(_fermiFrame, controlIndex, targetIndex);
    }

    public void Braid(int controlIndex, int targetIndex)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(controlIndex, MajoranaCount, nameof(controlIndex));
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(targetIndex, MajoranaCount, nameof(targetIndex));
        CliffordTransformUtility.ApplyBraid(_fermiFrame, controlIndex, targetIndex);
    }

    public void XError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            int xColumn = CliffordTransformUtility.GetPauliXColumn(qubitIndex);
            _qubitFrame[xColumn] = !_qubitFrame[xColumn];
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
            _qubitFrame[xColumn] = !_qubitFrame[xColumn];
            _qubitFrame[zColumn] = !_qubitFrame[zColumn];
        }
    }

    public void ZError(int qubitIndex, double probability)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            int zColumn = CliffordTransformUtility.GetPauliZColumn(qubitIndex);
            _qubitFrame[zColumn] = !_qubitFrame[zColumn];
        }
    }

    public void UError(int majoranaIndex, double probability)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            int xColumn = CliffordTransformUtility.GetMajoranaXColumn(majoranaIndex);
            _fermiFrame[xColumn] = !_fermiFrame[xColumn];
        }
    }

    public void VError(int majoranaIndex, double probability)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            int zColumn = CliffordTransformUtility.GetMajoranaZColumn(majoranaIndex);
            _fermiFrame[zColumn] = !_fermiFrame[zColumn];
        }
    }

    public void NError(int majoranaIndex, double probability)
    {
        PlatformArgumentUtility.ValidateMajoranaQubitIndex(majoranaIndex, MajoranaCount);
        PlatformArgumentUtility.ValidateProbability(probability);
        if (Random.Shared.NextDouble() < probability)
        {
            int xColumn = CliffordTransformUtility.GetMajoranaXColumn(majoranaIndex);
            int zColumn = CliffordTransformUtility.GetMajoranaZColumn(majoranaIndex);
            _fermiFrame[xColumn] = !_fermiFrame[xColumn];
            _fermiFrame[zColumn] = !_fermiFrame[zColumn];
        }
    }

    public void Reset(int qubitIndex)
    {
        PlatformArgumentUtility.ValidatePauliQubitIndex(qubitIndex, PauliCount);
        int xColumn = CliffordTransformUtility.GetPauliXColumn(qubitIndex);
        int zColumn = CliffordTransformUtility.GetPauliZColumn(qubitIndex);
        _qubitFrame[xColumn] = false;
        _qubitFrame[zColumn] = false;
        if (Random.Shared.NextDouble() < 0.5)
        {
            _qubitFrame[zColumn] = true;
        }
    }
}
