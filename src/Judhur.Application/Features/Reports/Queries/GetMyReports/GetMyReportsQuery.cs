using Judhur.Application.Common.Models;
using Judhur.Application.Features.Reports.Dto;
using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Reports.Queries.GetMyReports;

public sealed record GetMyReportsQuery(int Page, int PageSize) : IRequest<Result<PaginatedList<MyReportDto>>>;
