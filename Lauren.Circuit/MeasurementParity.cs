using System.Collections.Immutable;

namespace Lauren.Circuit;

public sealed record MeasurementParity(
    ImmutableArray<int> MeasurementIndices,
    bool Negated = false)
{
    public static MeasurementParity Empty { get; } = new(ImmutableArray<int>.Empty);
}