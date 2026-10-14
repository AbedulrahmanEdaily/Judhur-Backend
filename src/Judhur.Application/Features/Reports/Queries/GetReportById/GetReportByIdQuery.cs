using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Reports.Queries.GetReportById;

public sealed record GetReportByIdQuery(Guid ReportId) : IRequest<Result<ReportDetailsDto>>;
