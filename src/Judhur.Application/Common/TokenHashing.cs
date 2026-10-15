using System.Security.Cryptography;
using System.Text;

namespace Judhur.Application.Common;

public static class TokenHashing
{
    public const int HashLength = 64;

    public static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public static bool Matches(string expectedHash, string value)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expectedHash),
            Encoding.ASCII.GetBytes(Hash(value)));
}
