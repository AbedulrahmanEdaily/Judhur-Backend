namespace Judhur.Application.Common.Models;

public sealed record NewUserRegistration(
    string FullName,
    string Email,
    string PhoneNumber,
    string City,
    string? Bio,
    string Password);
