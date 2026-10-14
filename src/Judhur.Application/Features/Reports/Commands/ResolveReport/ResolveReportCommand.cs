using System.Text.Json.Serialization;

using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;

namespace Judhur.Application.Features.Reports.Commands.ResolveReport;

public sealed record ResolveReportCommand(
    [property: JsonIgnore] Guid ReportId,
    string? AdminNote,
    bool SuspendProperty,
    string? SuspensionReason) : IInvalidateCacheCommand<Result<Updated>>
{
    string[] IInvalidateCacheCommand.Tags => SuspendProperty ? ["properties"] : [];
}
