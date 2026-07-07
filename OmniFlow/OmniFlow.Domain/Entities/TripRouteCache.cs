using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class TripRouteCache : AuditableBaseEntity
{
    public Guid TripId { get; set; }
    public string RouteSignature { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public string Provider { get; set; } = "OpenRouteService";

    // Navigation properties
    public Trip Trip { get; set; } = null!;
}
