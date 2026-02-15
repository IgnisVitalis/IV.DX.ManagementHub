using Asp.Versioning;
using IV.DX.Hosting;
using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Contracts.Services;
using IV.ManagementHub.ApiService.Security;
using IV.ManagementHub.ApiService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
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
var bootstrapSettings = await bootstrapSettingsStore.LoadAsync();
var isBootstrapConfigured = bootstrapSettings?.IsConfigured == true;

if (isBootstrapConfigured)
{
    builder.Configuration["Database:Type"] = bootstrapSettings!.DatabaseType;
    builder.Configuration["Database:ConnectionString"] = bootstrapSettings.ConnectionString;
}

var rootAuthOptions = builder.Configuration.GetSection(RootAuthOptions.SectionName).Get<RootAuthOptions>() ?? new RootAuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rootAuthOptions.SigningKey));

builder.Services.AddSingleton(rootAuthOptions);
builder.Services.AddSingleton<RootTokenService>();
builder.Services.AddSingleton<IBootstrapSettingsStore>(bootstrapSettingsStore);
builder.Services.AddSingleton(new BootstrapRuntimeState());
builder.Services.AddSingleton<IBootstrapRuntimeActivator, BootstrapRuntimeActivator>();
builder.Services.AddSingleton<IBootstrapSetupService, BootstrapSetupService>();

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
builder.Services.AddDXPipeline();
builder.Services.AddDXInitializer();
builder.Services.AddScoped<IDXUnitStructureService, DXUnitStructureService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

var runtimeState = app.Services.GetRequiredService<BootstrapRuntimeState>();

app.Use(async (context, next) =>
{
    if (runtimeState.IsDxRuntimeEnabled)
    {
        await next();
        return;
    }

    var path = context.Request.Path;
    if (path.StartsWithSegments("/api/v1.0/setup") ||
        path.StartsWithSegments("/api/v1.0/auth") ||
        path.StartsWithSegments("/health") ||
        path.StartsWithSegments("/alive") ||
        path.StartsWithSegments("/openapi"))
    {
        await next();
        return;
    }

    if (!path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    await context.Response.WriteAsJsonAsync(new
    {
        error = "Service setup is not completed. Complete setup in UI."
    });
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (isBootstrapConfigured)
{
    var runtimeActivator = app.Services.GetRequiredService<IBootstrapRuntimeActivator>();
    var activationResult = await runtimeActivator.ActivateAsync();
    if (!activationResult.IsSuccess)
    {
        throw new InvalidOperationException($"DX runtime activation failed on startup. {activationResult.Message}");
    }
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
