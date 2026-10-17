namespace Judhur.Application.UnitTests.Validators;

public static class ValidationData
{
    public const string ValidPassword = "Passw0rd";
    public const string ValidEmail = "user@example.com";
    public const string ValidPhone = "0591234567";
    public const string ValidRowVersion = "AAAAAAAAB9E=";

    public static string Text(int length) => new('ب', length);

    public static string EmailOfLength(int length) => new string('a', length - "@example.com".Length) + "@example.com";

    public static TheoryData<string> ValidPhones =>
    [
        "0591234567",
        "0569876543",
        "970591234567",
        "+970591234567",
        "972521234567",
        "+972521234567",
    ];

    public static TheoryData<string> InvalidPhones =>
    [
        "059123456",
        "05912345678",
        "0691234567",
        "5591234567",
        "+0591234567",
        "971591234567",
        "+97059123456",
        "+9705912345678",
        "00970591234567",
        "059 1234567",
        "059-1234567",
        "05912345a7",
        "phone",
    ];

    public static TheoryData<string> ValidEmails =>
    [
        "user@example.com",
        "first.last+tag@sub.example.ps",
        "a@b",
    ];

    public static TheoryData<string> InvalidEmails =>
    [
        "plainaddress",
        "@example.com",
        "user@",
        "user@@example.com",
        "user@exa@mple.com",
    ];

    public static TheoryData<string> WeakPasswords =>
    [
        "Pass0rd",
        "passw0rd",
        "PASSW0RD",
        "Password",
        "Password١",
        "12345678",
    ];

    public static TheoryData<string> StrongPasswords =>
    [
        "Passw0rd",
        "aB3aaaaa",
        "كلمةSecret9",
    ];

    public static TheoryData<string?> InvalidRowVersions =>
    [
        (string?)null,
        "",
        "not base64!",
        "AAAAAA==",
        "AAAAAAAAAAAAAAAAAAAAAA==",
    ];
}
