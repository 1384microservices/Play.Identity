using System;

namespace Play.Identity.Service.Dtos;

public record UserDto(Guid Id, string UserName, string Email, decimal Gil, DateTimeOffset CreatedDate);
