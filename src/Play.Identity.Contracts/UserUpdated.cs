using System;

namespace Play.Identity.Contracts;

public record UserUpdated(Guid UserId, string Email, decimal NewTotalGil);