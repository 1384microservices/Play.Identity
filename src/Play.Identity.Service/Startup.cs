using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Play.Common.Configuration;
using Play.Common.Settings;
using Play.Identity.Service.Entities;
using Play.Identity.Service.HostedServices;
using Play.Identity.Service.Settings;
using Microsoft.AspNetCore.Identity;
using Play.Common.MassTransit;
using GreenPipes;
using Play.Identity.Service.Exceptions;

namespace Play.Identity.Service;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        var serviceSettings = Configuration.GetSection<ServiceSettings>();
        var mongoDbSettings = Configuration.GetSection<MongoDbSettings>();
        var identityServerSettings = Configuration.GetSection<IdentityServerSettings>();

        services
        .Configure<IdentitySettings>(Configuration.GetSection(nameof(IdentitySettings)))
        .AddDefaultIdentity<ApplicationUser>()
        .AddRoles<ApplicationRole>()
        .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(mongoDbSettings.ConnectionString, serviceSettings.Name)
        ;

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>>();
        services.AddMassTransitWithRabbitMQ(retryConfigurator =>
        {
            retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
            retryConfigurator.Ignore<InsufficientFundsException>();
            retryConfigurator.Ignore<UnknownUserException>();
        });

        services.AddIdentityServer(opt =>
        {
            opt.Events.RaiseSuccessEvents = true;
            opt.Events.RaiseFailureEvents = true;
            opt.Events.RaiseErrorEvents = true;
        })
        .AddAspNetIdentity<ApplicationUser>()
        .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
        .AddInMemoryApiResources(identityServerSettings.ApiResources)
        .AddInMemoryClients(identityServerSettings.Clients)
        .AddInMemoryIdentityResources(identityServerSettings.Resources)
        .AddDeveloperSigningCredential()
        ;

        services.AddLocalApiAuthentication();

        services.AddControllers();

        services.AddHostedService<IdentitySeedHostedService>();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Identity.Service", Version = "v1" });
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Identity.Service v1"));
            app.UseCors(opt =>
            {
                opt.WithOrigins(Configuration["AllowedOrigin"]);
                opt.AllowAnyHeader();
                opt.AllowAnyMethod();
            });
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
        });
    }
}
