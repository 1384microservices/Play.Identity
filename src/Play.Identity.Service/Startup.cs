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
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Reflection;
using Play.Common.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Cryptography.X509Certificates;

namespace Play.Identity.Service;

public class Startup
{
    private readonly IHostEnvironment environment;
    public IConfiguration Configuration { get; }


    public Startup(IConfiguration configuration, IHostEnvironment environment)
    {
        Configuration = configuration;
        this.environment = environment;
    }


    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        var serviceSettings = Configuration.GetSection<ServiceSettings>();
        var mongoDbSettings = Configuration.GetSection<MongoDbSettings>();


        services
            .Configure<IdentitySettings>(Configuration.GetSection(nameof(IdentitySettings)))
            .AddDefaultIdentity<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>(mongoDbSettings.ConnectionString, serviceSettings.Name);

        services
            .AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>>();

        services
            .AddMassTransitWithMessageBroker(Configuration, retryConfigurator =>
            {
                retryConfigurator.Interval(3, TimeSpan.FromSeconds(5));
                retryConfigurator.Ignore<InsufficientFundsException>();
                retryConfigurator.Ignore<UnknownUserException>();
            });

        ConfigureIdentity(services);

        services
            .AddLocalApiAuthentication();

        services
            .AddControllers();

        services
            .AddHostedService<IdentitySeedHostedService>();

        services
            .AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Identity.Service", Version = "v1" });
            });

        services
            .AddHealthChecks()
            .AddMongoDb();

        services
            .Configure<ForwardedHeadersOptions>(opt =>
            {
                opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                opt.KnownNetworks.Clear();
                opt.KnownProxies.Clear();
            });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseForwardedHeaders();

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

        // app.UseHttpsRedirection();

        app.Use((ctx, next) =>
        {
            var identitySettings = Configuration.GetSection<IdentitySettings>();
            ctx.Request.PathBase = new PathString(identitySettings.PathBase);

            return next();
        });

        app.UseStaticFiles();

        app.UseRouting();

        app.UseIdentityServer();

        app.UseAuthorization();

        app.UseCookiePolicy(new CookiePolicyOptions()
        {
            MinimumSameSitePolicy = SameSiteMode.Lax
        });

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapRazorPages();
            endpoints.MapPlayEconomyHealthChecks();
        });
    }

    private void ConfigureIdentity(IServiceCollection services)
    {
        var identityServerSettings = Configuration.GetSection<IdentityServerSettings>();

        var builder = services.
            AddIdentityServer(opt =>
            {
                opt.Events.RaiseSuccessEvents = true;
                opt.Events.RaiseFailureEvents = true;
                opt.Events.RaiseErrorEvents = true;
                opt.KeyManagement.KeyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddInMemoryApiScopes(identityServerSettings.ApiScopes)
            .AddInMemoryApiResources(identityServerSettings.ApiResources)
            .AddInMemoryClients(identityServerSettings.Clients)
            .AddInMemoryIdentityResources(identityServerSettings.Resources);

        if (environment.IsProduction())
        {
            var identitySettings = Configuration.GetSection<IdentitySettings>();
            var certificate = X509Certificate2.CreateFromPemFile(identitySettings.CertificateCerFilePath, identitySettings.CertificateKeyFilePath);
            builder.AddSigningCredential(certificate);
        }
        else
        {
            builder.AddDeveloperSigningCredential();
        }
    }
}
