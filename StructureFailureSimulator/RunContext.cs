using System;

namespace StructureFailureSimulator
{
    public class RunContext
    {
        public int Seed { get; }
        public Random RNG { get; }

        public int Day { get; set; }
        public float WindStrength { get; set; }
        public float SeismicPulse { get; set; }

        public RunContext(int seed)
        {
            Seed = seed;
            RNG = new Random(seed);
        }

        public void Advance()
        {
            Day++;

            // escalating difficulty curve
            WindStrength += 0.5f;
            SeismicPulse = (float)RNG.NextDouble() * WindStrength * 2f;
        }
    }
}