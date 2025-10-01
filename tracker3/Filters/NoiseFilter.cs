using System.Linq;
using Tracker.nn;
using System.Collections.Generic;

namespace Tracker.Filters
{
    // Beispiel-Implementierung eines PatternFilters: entfernt alle -1 Werte (Noise)
    public class NoiseFilter : PatternFilter
    {
        public override IO Apply(IO input)
        {
            var filtered = input.IOVECTOR.Where(id => id != -1).ToArray();
            return new IO { IOVECTOR = filtered };
        }
    }
}