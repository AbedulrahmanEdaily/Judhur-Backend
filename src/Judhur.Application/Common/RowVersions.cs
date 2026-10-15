namespace Judhur.Application.Common;

public static class RowVersions
{
    public const string PropertyName = "RowVersion";
    private const int Length = 8;

    public static bool TryParse(string? value, out byte[] rowVersion)
    {
        rowVersion = new byte[Length];
        return value is not null
            && Convert.TryFromBase64String(value, rowVersion, out var written)
            && written == Length;
    }

    public static string Format(byte[] rowVersion) => Convert.ToBase64String(rowVersion);
}
