using System.Collections.Immutable;

namespace Lauren.Circuit;

public sealed record PrototypeEntry(
    NoiseComponentKind NoiseKind,
    ImmutableArray<int> DetectorIndices,
    ImmutableArray<int> ObservableIndices);