namespace Lauren.Physics.Utility;

internal static class PlatformArgumentUtility
{
    public static void ValidatePauliQubitIndex(int qubitIndex, int pauliCount, string paramName = "qubitIndex")
    {
        if ((uint)qubitIndex >= (uint)pauliCount)
        {
            throw new ArgumentOutOfRangeException(paramName, "Qubit index is out of range.");
        }
    }

    public static void ValidateProbability(double probability, string paramName = "probability")
    {
        if (double.IsNaN(probability) || probability < 0d || probability > 1d)
        {
            throw new ArgumentOutOfRangeException(paramName, "Probability must be between 0 and 1.");
        }
    }

    public static void ValidateReferenceMeasurementValue(int referenceValue, string paramName = "referenceValue")
    {
        if (referenceValue is not 1 and not -1)
        {
            throw new ArgumentOutOfRangeException(paramName, "Reference measurement value must be +1 or -1.");
        }
    }
}