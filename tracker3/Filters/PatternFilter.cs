using Tracker.nn;
using Tracker.Filters;

namespace Tracker.Filters
{
    // Abstrakte Basisklasse für alle Pattern-Filter
    public abstract class PatternFilter
    {
        public abstract IO Apply(IO input);
    }
}