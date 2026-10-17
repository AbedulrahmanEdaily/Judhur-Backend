namespace Judhur.Tests.Common;

public static class TestClock
{
    public static DateTimeOffset UtcNow { get; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
}
