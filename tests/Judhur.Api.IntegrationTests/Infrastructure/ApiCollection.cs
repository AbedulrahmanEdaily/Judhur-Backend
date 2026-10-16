namespace Judhur.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<JudhurApiFactory>
{
    public const string Name = "api";
}
