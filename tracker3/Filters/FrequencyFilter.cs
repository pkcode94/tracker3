using Tracker.nn;

public class FrequencyFilter : PatternFilter
{
    private readonly int minFrequency;

    public FrequencyFilter(int minFrequency)
    {
        this.minFrequency = minFrequency;
    }

    public override IO Apply(IO input)
    {
        var freq = input.IOVECTOR.GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
        var filtered = input.IOVECTOR.Where(id => freq[id] >= minFrequency).ToArray();
        return new IO { IOVECTOR = filtered };
    }
}