using Tracker.nn;

public class DuplicateRemovalFilter : PatternFilter
{
    public override IO Apply(IO input)
    {
        var seen = new HashSet<int>();
        var filtered = input.IOVECTOR.Where(id => seen.Add(id)).ToArray();
        return new IO { IOVECTOR = filtered };
    }
}