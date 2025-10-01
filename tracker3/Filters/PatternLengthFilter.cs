using Tracker.nn;

public class PatternLengthFilter : PatternFilter
{
    private readonly int minLength;
    private readonly int maxLength;

    public PatternLengthFilter(int minLength, int maxLength)
    {
        this.minLength = minLength;
        this.maxLength = maxLength;
    }

    public override IO Apply(IO input)
    {
        if (input.IOVECTOR.Length < minLength || input.IOVECTOR.Length > maxLength)
            return new IO { IOVECTOR = Array.Empty<int>() };
        return new IO { IOVECTOR = input.IOVECTOR.ToArray() };
    }
}