
using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityService identityService,
    IEmailSender emailSender) : IRequestHandler<RegisterCommand, Result<Success>>
{

    private readonly IIdentityService _identityService = identityService;
    private readonly IEmailSender _emailSender = emailSender;

    public async Task<Result<Success>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var register = new NewUserRegistration(request.UserName, request.Email, request.PhoneNumber, request.Password, request.City, request.FullName, request.ProfileImageUrl, request.Bio);
        var userResult = await _identityService.CreateNewUserAsync(register, cancellationToken);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }
        var emailMessage = new EmailMessage(
            request.Email,
            "Confirm your email address",
            "Please confirm your email address to finish setting up your Judhur account.");
        await _emailSender.SendEmailAsync(emailMessage,cancellationToken);
        return Result.Success;
    }

}
