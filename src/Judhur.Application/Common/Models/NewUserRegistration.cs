namespace Judhur.Application.Common.Models;

public sealed record NewUserRegistration(
    string UserName,
    string Email,
    string PhoneNumber,
    string Password,
    string City,
    string FullName,
    string? ProfileImageUrl,
    string? Bio);
