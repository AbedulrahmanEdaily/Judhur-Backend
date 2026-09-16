using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Users;
using Judhur.Domain.Users.Enums;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IAppDbContext context,
    IIdentityService identityService,
    ITokenProvider tokenProvider) : IRequestHandler<RegisterCommand, Result<TokenResponse>>
{
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;

    public async Task<Result<TokenResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userResult = User.Create(Guid.CreateVersion7(), request.FullName, request.PhoneNumber, UserRole.User, request.City);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }
        var user = userResult.Value;
        var identityResult = await _identityService.CreateNewUserAsync(
            user.Id,
            new NewUserRegistration(request.UserName, request.Email, request.PhoneNumber, request.Password),
            cancellationToken);
        if (identityResult.IsError)
        {
            return identityResult.Errors;
        }
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return await _tokenProvider.GenerateJwtTokenAsync(identityResult.Value, cancellationToken);
    }
}
