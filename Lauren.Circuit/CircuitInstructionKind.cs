// ReSharper disable InconsistentNaming

namespace Lauren.Circuit;

public enum CircuitInstructionKind
{
    Trap,
    Tick,
    X,
    Y,
    Z,
    H,
    S,
    CX,
    Reset,
    MZ,
    MPP,
    Depolarize1,
    Depolarize2,
    Detector,
    ObservableInclude,
    PauliError,
    MeasurementError
}
