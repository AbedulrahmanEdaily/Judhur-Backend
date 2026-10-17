using Judhur.Application.Common.Behaviors;
using Judhur.Application.Common.Interfaces;
using Judhur.Application.UnitTests.TestDoubles;
using Judhur.Domain.Common.Results;

using NSubstitute;

namespace Judhur.Application.UnitTests.Behaviors;

public sealed class TransactionBehaviorTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITransactionScope _transaction = Substitute.For<ITransactionScope>();
    private readonly TransactionBehavior<TransactionalTestCommand, Result<string>> _behavior;

    public TransactionBehaviorTests()
    {
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>()).Returns(_transaction);
        _behavior = new TransactionBehavior<TransactionalTestCommand, Result<string>>(_unitOfWork);
    }

    [Fact]
    public async Task Handle_Success_CommitsAndDoesNotRollBack()
    {
        await _behavior.Handle(new TransactionalTestCommand("x"), _ => TestResults.Success(), TestContext.Current.CancellationToken);

        await _transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceiveWithAnyArgs().RollbackAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_Success_ReturnsHandlerResult()
    {
        var result = await _behavior.Handle(new TransactionalTestCommand("x"), _ => TestResults.Success("done"), TestContext.Current.CancellationToken);

        Assert.Equal("done", result.Value);
    }

    [Fact]
    public async Task Handle_Failure_RollsBackAndDoesNotCommit()
    {
        var result = await _behavior.Handle(new TransactionalTestCommand("x"), _ => TestResults.Failed(), TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        await _transaction.Received(1).RollbackAsync(Arg.Any<CancellationToken>());
        await _transaction.DidNotReceiveWithAnyArgs().CommitAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Handle_HandlerThrows_NeitherCommitsNorRollsBackExplicitly()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new TransactionalTestCommand("x"),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken));

        await _transaction.DidNotReceiveWithAnyArgs().CommitAsync(TestContext.Current.CancellationToken);
        await _transaction.DidNotReceiveWithAnyArgs().RollbackAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_AnyOutcome_DisposesTransaction(bool succeed)
    {
        await _behavior.Handle(
            new TransactionalTestCommand("x"),
            _ => succeed ? TestResults.Success() : TestResults.Failed(),
            TestContext.Current.CancellationToken);

        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Handle_HandlerThrows_DisposesTransaction()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _behavior.Handle(
            new TransactionalTestCommand("x"),
            _ => throw new InvalidOperationException("boom"),
            TestContext.Current.CancellationToken));

        await _transaction.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task Handle_Success_BeginsBeforeHandlerAndCommitsAfter()
    {
        var steps = new List<string>();
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { steps.Add("begin"); return _transaction; });
        _transaction.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { steps.Add("commit"); return Task.CompletedTask; });

        await _behavior.Handle(
            new TransactionalTestCommand("x"),
            _ => { steps.Add("handler"); return TestResults.Success(); },
            TestContext.Current.CancellationToken);

        Assert.Equal(["begin", "handler", "commit"], steps);
    }
}
