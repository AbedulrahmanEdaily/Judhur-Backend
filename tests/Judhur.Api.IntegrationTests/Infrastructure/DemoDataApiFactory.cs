namespace Judhur.Api.IntegrationTests.Infrastructure;

public sealed class DemoDataApiFactory : JudhurApiFactory
{
    protected override bool SeedDemoData => true;
}
