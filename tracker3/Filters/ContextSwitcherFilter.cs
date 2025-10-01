using Tracker.nn;

public class ContextSwitcherFilter : PatternFilter
{
    private readonly HashSet<int> triggerIds;
    private int currentContext = 0;

    public ContextSwitcherFilter(IEnumerable<int> triggerIds)
    {
        this.triggerIds = new HashSet<int>(triggerIds);
    }

    public int CurrentContext => currentContext;

    public override IO Apply(IO input)
    {
        foreach (var id in input.IOVECTOR)
        {
            if (triggerIds.Contains(id))
                currentContext++;
        }
        // Sie können hier die IOVECTOR je nach Kontext filtern oder markieren
        return new IO { IOVECTOR = input.IOVECTOR.ToArray() };
    }
}