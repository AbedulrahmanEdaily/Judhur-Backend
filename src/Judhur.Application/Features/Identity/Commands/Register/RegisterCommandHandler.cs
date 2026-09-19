using Judhur.Application.Common.Interfaces;
using Judhur.Application.Common.Models;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Identity.Commands.Register;

public sealed class RegisterCommandHandler(
    IAppDbContext context,
    IIdentityService identityService,
    ITokenProvider tokenProvider,
    IEmailSender emailSender) : IRequestHandler<RegisterCommand, Result<TokenResponse>>
{
    private readonly IAppDbContext _context = context;
    private readonly IIdentityService _identityService = identityService;
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    private readonly IEmailSender _emailSender = emailSender;

    public Task<Result<TokenResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

}
