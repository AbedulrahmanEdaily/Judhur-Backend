namespace Judhur.Application.Common.Models;

public sealed record NewUserRegistration(
    string UserName,
    string Email,
    string PhoneNumber,
    string Password);
