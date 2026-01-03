using System.Collections;

namespace Lauren.Physics;

public static class BitArrayExtensions
{
    extension(BitArray bitArray)
    {
        public int Weight => bitArray.Cast<bool>().Count(bit => bit);
    }
}