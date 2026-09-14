using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Judhur.Api.OpenApi.Transformer;

public sealed class VersionInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var version = context.DocumentName;
        document.Info.Version = version;
        document.Info.Title = $"Judhur API {version}";
        return Task.CompletedTask;
    }
}
