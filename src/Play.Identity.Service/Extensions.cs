using Play.Identity.Service.Dtos;
using Play.Identity.Service.Entities;

namespace Play.Identity.Service;

public static class Extensions
{
    public static UserDto AsDto(this ApplicationUser applicationUser)
    {
        var dto = new UserDto(applicationUser.Id, applicationUser.UserName, applicationUser.Email, applicationUser.Gil, applicationUser.CreatedOn);
        return dto;
    }
}