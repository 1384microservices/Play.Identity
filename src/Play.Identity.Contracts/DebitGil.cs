using System;

namespace Play.Identity.Contracts;

public record DebitGil(Guid UserId, decimal Gil, Guid CorrelationId);
