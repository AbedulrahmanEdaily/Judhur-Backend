namespace Judhur.Application.Common;

public static class Pagination
{
    public const int MaxPage = 10_000;

    public static bool IsPageValid(int page) => page is > 0 and <= MaxPage;
}
