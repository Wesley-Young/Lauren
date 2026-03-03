using System.Collections;
using Lauren.Physics.Operators;
using Lauren.Physics.Utility;

namespace Lauren.Physics.Platforms;

/// <summary>
///     Define a platform state evolution and measurement model based on the stabilizer representation,
///     supporting Pauli and Majorana gates, noise, measurement, and reset.
/// </summary>
public class Platform
{
    /// <summary>
    ///     Construct a platform with given numbers of Pauli qubits and Majorana fermi sites trapped.
    /// </summary>
    /// <param name="pauliCount">The number of Pauli qubits to trap.</param>
    /// <param name="majoranaCount">The number of Majorana fermi sites to trap.</param>
    public Platform(int pauliCount, int majoranaCount)
    {
        PauliStabilizers = new Stabilizer[pauliCount];
        for (int i = 0; i < pauliCount; i++)
        {
            PauliStabilizers[i] = new Stabilizer
            {
                Coefficient = Coefficient.PlusOne,
                Qubits = new BitArray(2 * pauliCount)
                {
                    [(2 * i) + 1] = true
                },
                FermiSites = new BitArray(2 * majoranaCount)
            };
        }

        MajoranaStabilizers = new Stabilizer[majoranaCount];
        for (int i = 0; i < majoranaCount; i++)
        {
            MajoranaStabilizers[i] = new Stabilizer
            {
                Coefficient = Coefficient.PlusI,
                Qubits = new BitArray(2 * pauliCount),
                FermiSites = new BitArray(2 * majoranaCount)
                {
                    [2 * i] = true,
                    [(2 * i) + 1] = true
                }
            };
        }

        _allStabilizers = new Stabilizer[pauliCount + majoranaCount];
        for (int i = 0; i < pauliCount; i++)
        {
            _allStabilizers[i] = PauliStabilizers[i];
        }
        for (int i = 0; i < majoranaCount; i++)
        {
            _allStabilizers[pauliCount + i] = MajoranaStabilizers[i];
        }
    }
    /// <summary>
    ///     Pauli stabilizers defining the platform.
    /// </summary>
    public Stabilizer[] PauliStabilizers { get; }

    /// <summary>
    ///     Majorana stabilizers defining the platform.
    /// </summary>
    public Stabilizer[] MajoranaStabilizers { get; }

    /// <summary>
    ///     Count of Pauli qubits in the platform.
    /// </summary>
    public int PauliCount => PauliStabilizers.Length;

    /// <summary>
    ///     Count of Fermi sites in the platform.
    /// </summary>
    public int MajoranaCount => MajoranaStabilizers.Length;

    private readonly Stabilizer[] _allStabilizers;

    /// <summary>
    ///     Measure a Hermitian operator on the platform.
    /// </summary>
    /// <param name="op">
    ///     The operator to measure, which must be Hermitian and supported by the platform's site counts.
    /// </param>
    /// <returns>+1 or -1, the outcome of the measurement.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown if the operator is not Hermitian or does not match the platform's qubit and
    ///     fermi site counts.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the operator is not in the span of the current stabilizers after
    ///     accounting for anti-commutation.
    /// </exception>
    public int Measure(QuantumOperator op)
    {
        if (!op.IsHermitian())
        {
            throw new ArgumentException("Measurement operator must be Hermitian.");
        }

        var measurement = StabilizerMeasurementUtility.BuildMeasurementStabilizer(op, PauliCount, MajoranaCount);

        int firstAnticommutingIndex = -1;
        for (int i = 0; i < _allStabilizers.Length; i++)
        {
            if (_allStabilizers[i].CommutesWith(measurement))
            {
                continue;
            }

            if (firstAnticommutingIndex == -1)
            {
                firstAnticommutingIndex = i;
                continue;
            }

            _allStabilizers[i].MultiplyInPlace(_allStabilizers[firstAnticommutingIndex]);
        }

        // Yield a random outcome if there is an anti-commuting stabilizer, and collapse the state accordingly.
        if (firstAnticommutingIndex != -1)
        {
            bool isPlusOutcome = Random.Shared.NextDouble() < 0.5;
            var collapsed = new Stabilizer
            {
                Coefficient = isPlusOutcome ? measurement.Coefficient : measurement.Coefficient * Coefficient.MinusOne,
                Qubits = (BitArray)measurement.Qubits.Clone(),
                FermiSites = (BitArray)measurement.FermiSites.Clone()
            };
            _allStabilizers[firstAnticommutingIndex].OverwriteWith(collapsed);
            return isPlusOutcome ? 1 : -1;
        }

        // If all stabilizers commute with the measurement, the outcome is deterministic, and we can solve for it.
        if (!StabilizerMeasurementUtility.TrySolveSpan(_allStabilizers, measurement, out bool[] solution))
        {
            throw new InvalidOperationException("Measured operator is not in the span of current stabilizers.");
        }

        var evaluated = StabilizerMeasurementUtility.MultiplySelected(_allStabilizers, solution);
        return evaluated.Coefficient == measurement.Coefficient ? 1 : -1;
    }
}