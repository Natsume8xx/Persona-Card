using System;

namespace PersonaCards.Core.Random
{
    public sealed class XorShift32Rng : ISeededRng
    {
        private const uint ZeroSeedFallback = 0x6D2B79F5u;

        private uint _state;

        public XorShift32Rng(uint seed)
        {
            _state = seed == 0u ? ZeroSeedFallback : seed;
        }

        public uint NextUInt()
        {
            var value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            _state = value;
            return value;
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(exclusiveMax),
                    exclusiveMax,
                    "Upper bound must be greater than zero.");
            }

            var bound = (uint)exclusiveMax;
            var threshold = unchecked(0u - bound) % bound;
            uint sample;

            do
            {
                sample = NextUInt();
            }
            while (sample < threshold);

            return (int)(sample % bound);
        }
    }
}
