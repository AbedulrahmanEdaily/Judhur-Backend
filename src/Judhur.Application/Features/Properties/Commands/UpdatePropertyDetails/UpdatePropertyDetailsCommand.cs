using System.Text.Json.Serialization;

using Judhur.Application.Common.Interfaces;
using Judhur.Domain.Common.Results;
using Judhur.Domain.Properties.Enums;

namespace Judhur.Application.Features.Properties.Commands.UpdatePropertyDetails;

public sealed record UpdatePropertyDetailsCommand(
    [property: JsonIgnore] Guid PropertyId,
    string Title,
    string? Description,
    decimal Price,
    PaymentType PaymentType,
    PropertyType PropertyType,
    decimal Area,
    string City,
    string? Region,
    string FullAddress,
    double Latitude,
    double Longitude,
    LandClassification LandClassification,
    LegalStatus LegalStatus
) : IInvalidateCacheCommand<Result<Success>>
{
    string[] IInvalidateCacheCommand.Tags => ["properties"];
}