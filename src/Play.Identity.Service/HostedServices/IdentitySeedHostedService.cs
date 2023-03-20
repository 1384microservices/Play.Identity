using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Settings;

namespace Play.Identity.Service.HostedServices;

public class IdentitySeedHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IdentitySettings _identitySettings;

    public IdentitySeedHostedService(IServiceScopeFactory serviceScopeFactory, IOptions<IdentitySettings> identitySettingsOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _identitySettings = identitySettingsOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await CreateRoleIfNotExistsAsync(Roles.Admin, roleManager);
        await CreateRoleIfNotExistsAsync(Roles.Player, roleManager);

        var adminUser = await userManager.FindByEmailAsync(_identitySettings.AdminUserEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = _identitySettings.AdminUserEmail,
                Email = _identitySettings.AdminUserEmail
            };

            await userManager.CreateAsync(adminUser, _identitySettings.AdminUserPassword);
            await userManager.AddToRoleAsync(adminUser, Roles.Admin);

        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task CreateRoleIfNotExistsAsync(string role, RoleManager<ApplicationRole> roleManager)
    {
        var roleExists = await roleManager.RoleExistsAsync(role);
        if (roleExists is false)
        {
            await roleManager.CreateAsync(new ApplicationRole()
            {
                Name = role
            });
        }
    }
}