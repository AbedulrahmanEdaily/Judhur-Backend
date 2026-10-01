namespace Judhur.Application.Common.Models;

public sealed record GoogleUser(string Subject, string Email, bool EmailVerified, string FullName, string? PictureUrl);