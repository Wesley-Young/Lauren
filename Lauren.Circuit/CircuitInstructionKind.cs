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
    Mz,
    Mpp,
    Depolarize1,
    Depolarize2,
    Detector,
    ObservableInclude,
    PauliError,
    MeasurementError
}