using System.Text.Json.Serialization;

using Judhur.Domain.Common.Results;

using MediatR;

namespace Judhur.Application.Features.Properties.Commands.RejectProperty;

public sealed record RejectPropertyCommand([property: JsonIgnore] Guid PropertyId, string RejectionReason) : IRequest<Result<Updated>>;