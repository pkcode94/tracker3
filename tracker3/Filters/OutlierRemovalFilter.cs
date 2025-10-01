using Tracker.nn;

public class OutlierRemovalFilter : PatternFilter
{
    public override IO Apply(IO input)
    {
        if (input.IOVECTOR.Length == 0)
            return new IO { IOVECTOR = Array.Empty<int>() };

        double avg = input.IOVECTOR.Average();
        double std = Math.Sqrt(input.IOVECTOR.Select(id => Math.Pow(id - avg, 2)).Average());
        var filtered = input.IOVECTOR.Where(id => Math.Abs(id - avg) <= 2 * std).ToArray();
        return new IO { IOVECTOR = filtered };
    }
}