using Asp.Versioning;
using IV.DX.Hosting;
using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Contracts.Services;
using IV.ManagementHub.ApiService.Security;
using IV.ManagementHub.ApiService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = null
        };
    });

builder.Services
    .AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(o =>
    {
        o.GroupNameFormat = "'v'VVV";
        o.SubstituteApiVersionInUrl = true;
    });

var bootstrapSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "bootstrap.settings.json");
var bootstrapSettingsStore = new JsonBootstrapSettingsStore(bootstrapSettingsPath);
var bootstrapSettings = (await bootstrapSettingsStore.LoadAsync())?.Normalize();

if (bootstrapSettings?.Instances.FirstOrDefault() is BootstrapInstanceSettings defaultInstance)
{
    builder.Configuration["Database:Type"] = defaultInstance.DatabaseType;
    builder.Configuration["Database:ConnectionString"] = defaultInstance.ConnectionString;
}

var rootAuthOptions = builder.Configuration.GetSection(RootAuthOptions.SectionName).Get<RootAuthOptions>() ?? new RootAuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rootAuthOptions.SigningKey));

builder.Services.AddSingleton(rootAuthOptions);
builder.Services.AddSingleton<RootTokenService>();
builder.Services.AddSingleton<IBootstrapSettingsStore>(bootstrapSettingsStore);
builder.Services.AddSingleton(new BootstrapSettingsSnapshot(bootstrapSettings));
builder.Services.AddSingleton(new BootstrapRuntimeState());
builder.Services.AddSingleton<IDatabaseRuntimeBinder, DatabaseRuntimeBinder>();
builder.Services.AddSingleton<IBootstrapRuntimeActivator, BootstrapRuntimeActivator>();
builder.Services.AddSingleton<IBootstrapSetupService, BootstrapSetupService>();
builder.Services.AddSingleton<IBootstrapInstanceService, BootstrapInstanceService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = rootAuthOptions.Issuer,
            ValidAudience = rootAuthOptions.Audience,
            IssuerSigningKey = signingKey,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.RootOnly, policy => policy.RequireRole(AuthRoles.Root));
});

builder.Services.AddDXCore(builder.Configuration);
ConfigureDynamicDxDatabaseOptions(builder.Services);
builder.Services.AddDXPipeline();
builder.Services.AddDXInitializer();
builder.Services.AddScoped<IDXUnitStructureService, DXUnitStructureService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

var runtimeState = app.Services.GetRequiredService<BootstrapRuntimeState>();
var settingsSnapshot = app.Services.GetRequiredService<BootstrapSettingsSnapshot>();
var runtimeBinder = app.Services.GetRequiredService<IDatabaseRuntimeBinder>();
var runtimeActivator = app.Services.GetRequiredService<IBootstrapRuntimeActivator>();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    if (path.StartsWithSegments("/api/v1.0/setup") ||
        path.StartsWithSegments("/api/v1.0/auth"))
    {
        await next();
        return;
    }

    var settings = settingsSnapshot.Current;
    if (settings?.IsConfigured != true)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Service setup is not completed. Complete setup in UI."
        });
        return;
    }

    if (path.StartsWithSegments("/api/v1.0/instances"))
    {
        await next();
        return;
    }

    var requestedInstanceKey = context.Request.Headers["X-MH-Instance"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(requestedInstanceKey))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Missing instance key. Send 'X-MH-Instance' header."
        });
        return;
    }

    var instance = settings.ResolveInstance(requestedInstanceKey);
    if (instance is null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            error = $"Instance '{requestedInstanceKey}' was not found."
        });
        return;
    }

    var isSwitchingInstance = !string.Equals(runtimeState.CurrentInstanceKey, instance.Key, StringComparison.OrdinalIgnoreCase);

    if (isSwitchingInstance)
    {
        var bindingResult = runtimeBinder.Bind(instance);
        if (!bindingResult.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"Failed to bind database settings for instance '{instance.Key}'. {bindingResult.Message}"
            });
            return;
        }

        runtimeState.MarkCurrentInstance(instance.Key);
    }

    if (isSwitchingInstance || !runtimeState.IsInstanceActivated(instance.Key))
    {
        var activationResult = await runtimeActivator.ActivateAsync(instance.Key, context.RequestAborted);
        if (!activationResult.IsSuccess)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = $"DX runtime activation failed for instance '{instance.Key}'. {activationResult.Message}"
            });
            return;
        }
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();

static void ConfigureDynamicDxDatabaseOptions(IServiceCollection services)
{
    var persistenceAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, "IV.DX.Persistence", StringComparison.Ordinal))
        ?? Assembly.Load("IV.DX.Persistence");

    var optionsType = persistenceAssembly.GetType("IV.DX.Persistence.DXDatabaseOptions");
    if (optionsType is null)
    {
        return;
    }

    var iOptionsType = typeof(IOptions<>).MakeGenericType(optionsType);
    var optionsWrapperType = typeof(OptionsWrapper<>).MakeGenericType(optionsType);
    var typeProperty = optionsType.GetProperty("Type");
    var connectionStringProperty = optionsType.GetProperty("ConnectionString");

    services.Replace(ServiceDescriptor.Transient(iOptionsType, serviceProvider =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var optionsInstance = Activator.CreateInstance(optionsType)
            ?? throw new InvalidOperationException("Unable to create DXDatabaseOptions instance.");

        typeProperty?.SetValue(optionsInstance, configuration["Database:Type"]);
        connectionStringProperty?.SetValue(optionsInstance, configuration["Database:ConnectionString"]);

        return Activator.CreateInstance(optionsWrapperType, optionsInstance)
            ?? throw new InvalidOperationException("Unable to create DX database options wrapper.");
    }));
}
