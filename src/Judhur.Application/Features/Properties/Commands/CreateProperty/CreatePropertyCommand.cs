using Judhur.Application.Features.Properties.Dto;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

using MediatR;

namespace Judhur.Application.Features.Properties.Commands.CreateProperty;

public sealed record CreatePropertyCommand(
    string Title,
    string? Description,
    decimal Price,
    PaymentType PaymentType,
    PropertyType PropertyType,
    PropertyStatus PropertyStatus,
    decimal Area,
    string City,
    string? Region,
    string FullAddress,
    double Latitude,
    double Longitude,
    LandClassification LandClassification,
    LegalStatus LegalStatus) : IRequest<Result<PropertyDto>>;