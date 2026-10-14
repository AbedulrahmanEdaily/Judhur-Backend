using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports.Enums;

using MediatR;

namespace Judhur.Application.Features.Reports.Commands.CreateReport;

public sealed record CreateReportCommand(Guid PropertyId, ReportReason Reason, string? Details) : IRequest<Result<ReportCreatedDto>>;
