namespace Judhur.Application.Features.Properties.Dto;

public sealed record PropertyImageDto(Guid Id, string Url, int DisplayOrder, bool IsMainImage);