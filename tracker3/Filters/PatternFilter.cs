using Tracker.nn;
using Tracker.Filters;

namespace Tracker.Filters
{
    // Abstrakte Basisklasse f�r alle Pattern-Filter
    public abstract class PatternFilter
    {
        public abstract IO Apply(IO input);
    }
}