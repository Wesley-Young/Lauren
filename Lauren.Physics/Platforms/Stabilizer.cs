using System.Collections;

namespace Lauren.Physics.Platforms;

public class Stabilizer
{
    public Coefficient Coefficient { get; set; }

    public required BitArray Qubits { get; init; }

    public required BitArray FermiSites { get; init; }
}