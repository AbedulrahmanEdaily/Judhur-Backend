
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IEmailSender emailSender) : IRequestHandler<RegisterCommand, Result<Success>>
{

    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IEmailSender _emailSender = emailSender;

    public async Task<Result<Success>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var register = new NewUserRegistration(request.UserName, request.Email, request.PhoneNumber, request.Password, request.City, request.FullName, request.ProfileImageUrl, request.Bio);
        var userResult = await _identityService.CreateNewUserAsync(register, cancellationToken);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }
        var response = await _tokenProvider.GenerateJwtTokenAsync(userResult.Value);
        if (response.IsError)
        {
            return response.Errors;
        }
        var emailMessage = new EmailMessage("admin@judhur.com","","please confirm your email");
        await _emailSender.SendEmailAsync(emailMessage,cancellationToken);
        return Result.Success;
    }

}
