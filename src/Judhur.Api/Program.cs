using Asp.Versioning.ApiExplorer;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddApi();

var app = builder.Build();

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

app.UseCoreMiddlewares(builder.Configuration);
app.MapControllers();

app.Run();
