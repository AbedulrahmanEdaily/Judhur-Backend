namespace Judhur.Api;

public static class RequestLimits
{
    public const long DefaultBodySize = 1 * 1024 * 1024;
    public const long ImageUploadBodySize = 6 * 1024 * 1024;
    public const long DocumentUploadBodySize = 11 * 1024 * 1024;
}
