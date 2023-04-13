using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Play.Identity.Contracts;
using Play.Identity.Service.Dtos;
using Play.Identity.Service.Entities;
using static Duende.IdentityServer.IdentityServerConstants;

namespace Play.Identity.Service.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = Roles.Admin, Policy = LocalApi.PolicyName)]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPublishEndpoint _publishEndpoint;

    public UsersController(UserManager<ApplicationUser> userManager, IPublishEndpoint publishEndpoint)
    {
        this._userManager = userManager;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserDto>> Get()
    {
        var theClaim = User.Claims;

        var users = _userManager.Users.ToList().Select(u => u.AsDto());
        return Ok(users);
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user.AsDto());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        user.Email = dto.Email;
        user.UserName = dto.Email;
        user.Gil = dto.Gil;

        await _userManager.UpdateAsync(user);
        await _publishEndpoint.Publish(new UserUpdated(user.Id, user.Email, user.Gil));

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        await _userManager.DeleteAsync(user);
        await _publishEndpoint.Publish(new UserUpdated(user.Id, user.Email, 0));
        return NoContent();
    }
}