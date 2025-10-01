using Tracker.nn;

public class NormalizationFilter : PatternFilter
{
    public override IO Apply(IO input)
    {
        if (input.IOVECTOR.Length == 0)
            return new IO { IOVECTOR = Array.Empty<int>() };

        int min = input.IOVECTOR.Min();
        int max = input.IOVECTOR.Max();
        if (max == min)
            return new IO { IOVECTOR = input.IOVECTOR.ToArray() };

        // Normierung auf Bereich 0–100
        var normalized = input.IOVECTOR
            .Select(id => (int)Math.Round(100.0 * (id - min) / (max - min)))
            .ToArray();
        return new IO { IOVECTOR = normalized };
    }
}