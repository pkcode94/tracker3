using Tracker.nn;

public class SlidingWindowFilter : PatternFilter
{
    private readonly int windowSize;
    private readonly int step;

    public SlidingWindowFilter(int windowSize, int step = 1)
    {
        this.windowSize = windowSize;
        this.step = step;
    }

    public override IO Apply(IO input)
    {
        var windows = new List<int>();
        for (int i = 0; i <= input.IOVECTOR.Length - windowSize; i += step)
        {
            windows.AddRange(input.IOVECTOR.Skip(i).Take(windowSize));
        }
        return new IO { IOVECTOR = windows.ToArray() };
    }
}