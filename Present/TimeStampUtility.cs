using X10D.Time;

namespace Present;

internal static class TimeStampUtility
{
    public static bool TryParse(ReadOnlySpan<char> input, out DateTimeOffset result)
    {
        if (long.TryParse(input, out long endTimestamp))
        {
            result = DateTimeOffset.FromUnixTimeSeconds(endTimestamp);
            return true;
        }

        if (TimeSpanParser.TryParse(input, out TimeSpan endRelative))
        {
            result = DateTimeOffset.UtcNow + endRelative;
            return true;
        }

        result = default;
        return false;
    }
}
