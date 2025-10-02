using System;
using System.Linq;
using Tracker.nn;
using System.Collections.Generic;

namespace Tracker.Filters
{
    // Filter that adds random noise to pattern values
    public class NoiseFilter : PatternFilter
    {
        private readonly int noiseLevel;
        private readonly Random random = new();

        public NoiseFilter(int noiseLevel = 1)
        {
            this.noiseLevel = noiseLevel;
        }

        public override IO Apply(IO input)
        {
            var output = new IO { IOVECTOR = new int[input.IOVECTOR.Length] };
            for (int i = 0; i < input.IOVECTOR.Length; i++)
            {
                output.IOVECTOR[i] = input.IOVECTOR[i] + random.Next(-noiseLevel, noiseLevel + 1);
            }
            return output;
        }
    }
}