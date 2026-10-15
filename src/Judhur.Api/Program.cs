using Asp.Versioning.ApiExplorer;

using Judhur.Api;
using Judhur.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = RequestLimits.DefaultBodySize);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeadersIfEnabled(builder.Configuration);
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    var apiVersions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(options =>
    {
        foreach (var description in apiVersions.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/openapi/{description.GroupName}.json",
                $"Judhur API {description.GroupName}");
        }
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
    });
}
else
{
    app.UseHsts();
}
await app.InitializeDatabaseAsync();
app.UseCoreMiddlewares(builder.Configuration);
app.MapControllers();
app.MapRealtime();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
