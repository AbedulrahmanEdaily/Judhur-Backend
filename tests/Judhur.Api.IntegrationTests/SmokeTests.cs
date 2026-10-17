using System.Net;

using Judhur.Api.IntegrationTests.Infrastructure;

namespace Judhur.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SmokeTests(JudhurApiFactory factory)
{
    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var response = await factory.CreateClient().GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_SeededAccount_CanLogIn()
    {
        var admin = await new Scenario(factory).AdminAsync();

        Assert.NotEqual(Guid.Empty, admin.Id);
    }
}
