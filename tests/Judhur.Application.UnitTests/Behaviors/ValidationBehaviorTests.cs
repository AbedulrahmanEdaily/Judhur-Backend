using FluentValidation;

using Judhur.Application.Common.Behaviors;
using Judhur.Application.Features.Identity;
using Judhur.Application.Features.Identity.Commands.Login;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class ValidationBehaviorTests
{
    private static InlineValidator<PlainTestCommand> NameRequired()
    {
        var validator = new InlineValidator<PlainTestCommand>();
        validator.RuleFor(c => c.Name).NotEmpty().WithMessage("الاسم مطلوب.");
        return validator;
    }

    [Fact]
    public async Task Handle_NoValidator_CallsNext()
    {
        var behavior = new ValidationBehavior<PlainTestCommand, Result<string>>();
        var calls = 0;

        var result = await behavior.Handle(new PlainTestCommand(""), _ => { calls++; return TestResults.Success("next"); }, TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        Assert.Equal("next", result.Value);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<PlainTestCommand, Result<string>>(NameRequired());
        var calls = 0;

        var result = await behavior.Handle(new PlainTestCommand("Ali"), _ => { calls++; return TestResults.Success("next"); }, TestContext.Current.CancellationToken);

        Assert.Equal(1, calls);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_InvalidRequest_DoesNotCallNext()
    {
        var behavior = new ValidationBehavior<PlainTestCommand, Result<string>>(NameRequired());
        var calls = 0;

        await behavior.Handle(new PlainTestCommand(""), _ => { calls++; return TestResults.Success(); }, TestContext.Current.CancellationToken);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsValidationErrorKeyedByPropertyName()
    {
        var behavior = new ValidationBehavior<PlainTestCommand, Result<string>>(NameRequired());

        var result = await behavior.Handle(new PlainTestCommand(""), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(PlainTestCommand.Name), error.Code);
        Assert.Equal("الاسم مطلوب.", error.Description);
        Assert.Equal(ErrorKind.Validation, error.Type);
    }

    [Fact]
    public async Task Handle_SeveralFailures_ReturnsOneErrorPerFailure()
    {
        var behavior = new ValidationBehavior<LoginCommand, Result<TokenResponse>>(new LoginCommandValidator());

        var result = await behavior.Handle(
            new LoginCommand("", ""),
            _ => throw new InvalidOperationException("next must not run"),
            TestContext.Current.CancellationToken);

        Assert.Equal(
            [nameof(LoginCommand.Email), nameof(LoginCommand.Password)],
            result.Errors.Select(e => e.Code).ToArray());
        Assert.All(result.Errors, e => Assert.Equal(ErrorKind.Validation, e.Type));
    }
}
