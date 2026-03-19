using IV.ManagementHub.ApiService.Bootstrap;
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

var bootstrapSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "bootstrap.settings.json");
var bootstrapSettingsStore = new JsonBootstrapSettingsStore(bootstrapSettingsPath);
var bootstrapSettings = (await bootstrapSettingsStore.LoadAsync())?.Normalize();

var rootAuthOptions = builder.Configuration.GetSection(RootAuthOptions.SectionName).Get<RootAuthOptions>() ?? new RootAuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rootAuthOptions.SigningKey));

builder.Services.AddSingleton(rootAuthOptions);
builder.Services.AddSingleton<RootTokenService>();
builder.Services.AddSingleton<IBootstrapSettingsStore>(bootstrapSettingsStore);
builder.Services.AddSingleton(new BootstrapSettingsSnapshot(bootstrapSettings));
builder.Services.AddSingleton<IBootstrapSetupService, BootstrapSetupService>();
builder.Services.AddSingleton<IBootstrapInstanceService, BootstrapInstanceService>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<InstanceApiClientFactory>();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

var settingsSnapshot = app.Services.GetRequiredService<BootstrapSettingsSnapshot>();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    if (path.StartsWithSegments("/api/setup") ||
        path.StartsWithSegments("/api/auth"))
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

    if (path.StartsWithSegments("/api/instances"))
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

    // Set per-request API context (AsyncLocal — each request flow gets its own copy,
    // so concurrent requests for different instances cannot interfere with each other).
    InstanceApiContext.Set(instance);

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
