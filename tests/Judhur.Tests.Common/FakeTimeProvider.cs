namespace Judhur.Tests.Common;

public sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow;

    public FakeTimeProvider()
        : this(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);

    public override DateTimeOffset GetUtcNow() => _utcNow;
}
