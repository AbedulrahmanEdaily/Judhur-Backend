using System.Text.Json.Serialization;

using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Reports.Commands.DismissReport;

public sealed record DismissReportCommand([property: JsonIgnore] Guid ReportId, string? AdminNote) : IRequest<Result<Updated>>;
