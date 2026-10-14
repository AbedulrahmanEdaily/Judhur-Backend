using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Reports.Enums;

using MediatR;

namespace Judhur.Application.Features.Reports.Queries.GetReports;

public sealed record GetReportsQuery(ReportStatus Status, int Page, int PageSize) : IRequest<Result<PaginatedList<ReportSummaryDto>>>;
