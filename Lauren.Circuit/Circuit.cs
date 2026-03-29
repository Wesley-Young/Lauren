using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Lauren.Physics.Operators;

namespace Lauren.Circuit;

public sealed class Circuit
{
    private readonly List<CircuitInstruction> _instructions = [];
    private readonly List<CircuitInstruction> _normalizedInstructions = [];
    private readonly List<int> _measurementInstructionIndices = [];
    private readonly List<int> _noiseInstructionIndices = [];
    private readonly List<MeasurementParity> _detectors = [];
    private readonly List<MeasurementParity> _observables = [];

    private int[]? _referenceMeasurements;
    private IReadOnlyList<PrototypeEntry>? _prototype;
    private bool _hasTrap;

    public int PauliCount { get; private set; }

    public IReadOnlyList<CircuitInstruction> Instructions => new ReadOnlyCollection<CircuitInstruction>(_instructions);

    public IReadOnlyList<CircuitInstruction> NormalizedInstructions =>
        new ReadOnlyCollection<CircuitInstruction>(_normalizedInstructions);

    public IReadOnlyList<int> MeasurementInstructionIndices =>
        new ReadOnlyCollection<int>(_measurementInstructionIndices);

    public IReadOnlyList<int> NoiseInstructionIndices =>
        new ReadOnlyCollection<int>(_noiseInstructionIndices);

    public IReadOnlyList<MeasurementParity> Detectors => new ReadOnlyCollection<MeasurementParity>(_detectors);

    public IReadOnlyList<MeasurementParity> Observables => new ReadOnlyCollection<MeasurementParity>(_observables);

    public IReadOnlyList<int>? CachedReferenceMeasurements => _referenceMeasurements;

    public IReadOnlyList<PrototypeEntry>? CachedPrototype => _prototype;

    public void Trap(int pauliCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(pauliCount);

        if (_hasTrap)
        {
            throw new InvalidOperationException("TRAP can only be appended once.");
        }

        InvalidateCaches();

        var instruction = CircuitInstruction.Create(CircuitInstructionKind.Trap, pauliCount);
        _instructions.Add(instruction);
        _normalizedInstructions.Add(instruction);
        PauliCount = pauliCount;
        _hasTrap = true;
    }

    public void Tick()
    {
        InvalidateCaches();

        var instruction = CircuitInstruction.Create(CircuitInstructionKind.Tick);
        _instructions.Add(instruction);
        _normalizedInstructions.Add(instruction);
    }

    public void X(int qubitIndex) => AppendSingleQubitGate(CircuitInstructionKind.X, qubitIndex);

    public void Y(int qubitIndex) => AppendSingleQubitGate(CircuitInstructionKind.Y, qubitIndex);

    public void Z(int qubitIndex) => AppendSingleQubitGate(CircuitInstructionKind.Z, qubitIndex);

    public void H(int qubitIndex) => AppendSingleQubitGate(CircuitInstructionKind.H, qubitIndex);

    public void S(int qubitIndex) => AppendSingleQubitGate(CircuitInstructionKind.S, qubitIndex);

    public void Reset(int qubitIndex) => AppendSingleQubitGate(CircuitInstructionKind.Reset, qubitIndex);

    public void CX(int controlIndex, int targetIndex)
    {
        EnsureTrapAppended();
        ValidateQubitIndex(controlIndex, nameof(controlIndex));
        ValidateQubitIndex(targetIndex, nameof(targetIndex));

        InvalidateCaches();

        var instruction = CircuitInstruction.Create(CircuitInstructionKind.CX, controlIndex, targetIndex);
        _instructions.Add(instruction);
        _normalizedInstructions.Add(instruction);
    }

    public void Mpp(PauliOperator target, double measurementErrorProbability = 0.0)
    {
        EnsureTrapAppended();
        ArgumentNullException.ThrowIfNull(target);
        ValidateProbability(measurementErrorProbability, nameof(measurementErrorProbability));
        ValidatePauliTarget(target, nameof(target));

        InvalidateCaches();

        var rawInstruction = CircuitInstruction.CreateMpp(target, measurementErrorProbability);
        _instructions.Add(rawInstruction);

        int measurementIndex = _normalizedInstructions.Count;
        _normalizedInstructions.Add(CircuitInstruction.CreateMpp(target));
        _measurementInstructionIndices.Add(measurementIndex);

        if (measurementErrorProbability > 0d)
        {
            int noiseIndex = _normalizedInstructions.Count;
            _normalizedInstructions.Add(
                new CircuitInstruction(
                    CircuitInstructionKind.MeasurementError,
                    ImmutableArray<int>.Empty,
                    Probability: measurementErrorProbability,
                    NoiseKind: NoiseComponentKind.MeasurementError));
            _noiseInstructionIndices.Add(noiseIndex);
        }
    }

    public void Mz(int qubitIndex, double measurementErrorProbability = 0.0)
    {
        EnsureTrapAppended();
        ValidateQubitIndex(qubitIndex, nameof(qubitIndex));
        ValidateProbability(measurementErrorProbability, nameof(measurementErrorProbability));

        InvalidateCaches();

        _instructions.Add(
            CircuitInstruction.Create(
                CircuitInstructionKind.Mz,
                measurementErrorProbability,
                qubitIndex
            )
        );
        AppendNormalizedMeasurement(
            CreatePauli((qubitIndex, X: false, Z: true)),
            measurementErrorProbability);
    }

    public void Depolarize1(int qubitIndex, double probability)
    {
        EnsureTrapAppended();
        ValidateQubitIndex(qubitIndex, nameof(qubitIndex));
        ValidateProbability(probability, nameof(probability));

        InvalidateCaches();

        _instructions.Add(CircuitInstruction.Create(CircuitInstructionKind.Depolarize1, probability, qubitIndex));

        AppendNormalizedPauliError(
            CreatePauli((qubitIndex, X: true, Z: false)),
            probability,
            NoiseComponentKind.Depolarize1Component);
        AppendNormalizedPauliError(
            CreatePauli((qubitIndex, X: true, Z: true)),
            probability,
            NoiseComponentKind.Depolarize1Component);
        AppendNormalizedPauliError(
            CreatePauli((qubitIndex, X: false, Z: true)),
            probability,
            NoiseComponentKind.Depolarize1Component);
    }

    public void Depolarize2(int firstQubitIndex, int secondQubitIndex, double probability)
    {
        EnsureTrapAppended();
        ValidateQubitIndex(firstQubitIndex, nameof(firstQubitIndex));
        ValidateQubitIndex(secondQubitIndex, nameof(secondQubitIndex));
        ValidateProbability(probability, nameof(probability));

        InvalidateCaches();

        _instructions.Add(
            CircuitInstruction.Create(
                CircuitInstructionKind.Depolarize2,
                probability,
                firstQubitIndex,
                secondQubitIndex));

        foreach (PauliOperator pauli in EnumerateTwoQubitDepolarizingComponents(firstQubitIndex, secondQubitIndex))
        {
            AppendNormalizedPauliError(pauli, probability, NoiseComponentKind.Depolarize2Component);
        }
    }

    public void Detector(params int[] measurementReferences)
    {
        ArgumentNullException.ThrowIfNull(measurementReferences);

        InvalidateCaches();

        _instructions.Add(CircuitInstruction.Create(CircuitInstructionKind.Detector, measurementReferences));
        _detectors.Add(ResolveMeasurementParity(measurementReferences));
    }

    public void ObservableInclude(params int[] measurementReferences)
    {
        ArgumentNullException.ThrowIfNull(measurementReferences);

        InvalidateCaches();

        _instructions.Add(CircuitInstruction.Create(CircuitInstructionKind.ObservableInclude, measurementReferences));
        _observables.Add(ResolveMeasurementParity(measurementReferences));
    }

    internal void SetCachedReferenceMeasurements(int[]? referenceMeasurements)
    {
        _referenceMeasurements = referenceMeasurements;
    }

    internal void SetCachedPrototype(IReadOnlyList<PrototypeEntry>? prototype)
    {
        _prototype = prototype;
    }

    internal void InvalidateCaches()
    {
        _referenceMeasurements = null;
        _prototype = null;
    }

    private void AppendSingleQubitGate(CircuitInstructionKind kind, int qubitIndex)
    {
        EnsureTrapAppended();
        ValidateQubitIndex(qubitIndex, nameof(qubitIndex));

        InvalidateCaches();

        var instruction = CircuitInstruction.Create(kind, qubitIndex);
        _instructions.Add(instruction);
        _normalizedInstructions.Add(instruction);
    }

    private void AppendNormalizedMeasurement(PauliOperator target, double measurementErrorProbability)
    {
        int measurementIndex = _normalizedInstructions.Count;
        _normalizedInstructions.Add(CircuitInstruction.CreateMpp(target));
        _measurementInstructionIndices.Add(measurementIndex);

        if (measurementErrorProbability > 0d)
        {
            int noiseIndex = _normalizedInstructions.Count;
            _normalizedInstructions.Add(
                new CircuitInstruction(
                    CircuitInstructionKind.MeasurementError,
                    ImmutableArray<int>.Empty,
                    Probability: measurementErrorProbability,
                    NoiseKind: NoiseComponentKind.MeasurementError));
            _noiseInstructionIndices.Add(noiseIndex);
        }
    }

    private void AppendNormalizedPauliError(PauliOperator noisePauli, double probability, NoiseComponentKind noiseKind)
    {
        int noiseIndex = _normalizedInstructions.Count;
        _normalizedInstructions.Add(CircuitInstruction.CreatePauliError(noisePauli, probability, noiseKind));
        _noiseInstructionIndices.Add(noiseIndex);
    }

    private MeasurementParity ResolveMeasurementParity(IReadOnlyList<int> measurementReferences)
    {
        if (measurementReferences.Count == 0)
        {
            throw new ArgumentException("At least one measurement reference is required.", nameof(measurementReferences));
        }

        int measurementCount = _measurementInstructionIndices.Count;
        var resolved = ImmutableArray.CreateBuilder<int>(measurementReferences.Count);

        foreach (int reference in measurementReferences)
        {
            int absoluteIndex = reference >= 0 ? reference : measurementCount + reference;
            if ((uint)absoluteIndex >= (uint)measurementCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(measurementReferences),
                    $"Measurement reference {reference} resolves outside the valid range [0, {measurementCount - 1}].");
            }

            resolved.Add(absoluteIndex);
        }

        return new MeasurementParity(resolved.MoveToImmutable());
    }

    private IEnumerable<PauliOperator> EnumerateTwoQubitDepolarizingComponents(int firstQubitIndex, int secondQubitIndex)
    {
        yield return CreatePauli((firstQubitIndex, X: true, Z: false));
        yield return CreatePauli((firstQubitIndex, X: true, Z: true));
        yield return CreatePauli((firstQubitIndex, X: false, Z: true));

        yield return CreatePauli((secondQubitIndex, X: true, Z: false));
        yield return CreatePauli((secondQubitIndex, X: true, Z: true));
        yield return CreatePauli((secondQubitIndex, X: false, Z: true));

        foreach ((bool firstX, bool firstZ, bool secondX, bool secondZ) combination in new[]
                 {
                     (true, false, true, false),
                     (true, false, true, true),
                     (true, false, false, true),
                     (true, true, true, false),
                     (true, true, true, true),
                     (true, true, false, true),
                     (false, true, true, false),
                     (false, true, true, true),
                     (false, true, false, true)
                 })
        {
            yield return CreatePauli(
                (firstQubitIndex, combination.firstX, combination.firstZ),
                (secondQubitIndex, combination.secondX, combination.secondZ));
        }
    }

    private PauliOperator CreatePauli(params (int QubitIndex, bool X, bool Z)[] terms)
    {
        var occupiedX = new BitArray(PauliCount);
        var occupiedZ = new BitArray(PauliCount);

        foreach ((int qubitIndex, bool x, bool z) in terms)
        {
            occupiedX[qubitIndex] = x;
            occupiedZ[qubitIndex] = z;
        }

        return PauliOperator.CreateHermitian(occupiedX, occupiedZ);
    }

    private void ValidatePauliTarget(PauliOperator target, string paramName)
    {
        if (!target.IsHermitian())
        {
            throw new ArgumentException("Measurement target must be Hermitian.", paramName);
        }

        if (target.OccupiedX.Length != PauliCount)
        {
            throw new ArgumentException("Measurement target size must match circuit pauli count.", paramName);
        }
    }

    private void EnsureTrapAppended()
    {
        if (!_hasTrap)
        {
            throw new InvalidOperationException("TRAP must be appended before qubit operations.");
        }
    }

    private void ValidateQubitIndex(int qubitIndex, string paramName)
    {
        if ((uint)qubitIndex >= (uint)PauliCount)
        {
            throw new ArgumentOutOfRangeException(paramName, "Qubit index is out of range.");
        }
    }

    private static void ValidateProbability(double probability, string paramName)
    {
        if (double.IsNaN(probability) || probability < 0d || probability > 1d)
        {
            throw new ArgumentOutOfRangeException(paramName, "Probability must be between 0 and 1.");
        }
    }
}