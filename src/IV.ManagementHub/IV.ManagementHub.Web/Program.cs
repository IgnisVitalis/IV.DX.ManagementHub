using System.Collections.Concurrent;
using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DataProvider.WebApp.Services.Web.Services;
using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Hosting;
using IV.DX.Kernel.Models;
using IV.DX.Presentation.Hosting;
using IV.DX.WebApi.Auth.DependencyInjection;
using IV.DX.WebApi.DependencyInjection;
using IV.DX.WebApi.Management.DependencyInjection;
using IV.ManagementHub.ApiService.Bootstrap;
using IV.ManagementHub.ApiService.Controllers;  // DXApiControllerBase assembly
using IV.ManagementHub.ApiService.Services;
using IV.ManagementHub.Common.Models;
using IV.ManagementHub.Web.Components;
using IV.ManagementHub.Web.Services;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// --- DX Core ---
builder.Services
    .AddDX(builder.Configuration)
    .AddSecurity()
    .RegisterHostedService();

builder.Services.AddDXHandlers(typeof(Program).Assembly);

// --- DX Presentation ---
builder.Services
    .AddDXPresentation()
    .RegisterHostedService();

// --- MH Presentation data (runs after DX Presentation types are defined) ---
builder.Services.AddHostedService<MHCustomDataHostedService>();

// --- DX Management API (service key auth + dx-system policy) ---
builder.Services.AddDXWebApiManagement();

// --- DX JWT auth ---
builder.Services.AddDXWebApiJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddDXWebApiDefaults();

// --- Controllers ---
builder.Services.AddControllers()
    .AddDXWebApiAuthControllers()           // api/auth/* (DX login/refresh/logout)
    .AddDXManagementControllers()           // api/management/* (DX CRUD, adds Newtonsoft)
    .AddApplicationPart(typeof(DXApiControllerBase).Assembly);  // MH proxy controllers

// --- DX Rate limiting ---
builder.Services.AddDXWebApiRateLimiting(builder.Configuration);

// --- Blazor ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddOutputCache();

// --- MH services ---
builder.Services.AddHttpClient();
builder.Services.AddSingleton<InstanceApiClientFactory>();

builder.Services.AddScoped<IApiClientResolver, ApiClientResolver>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<AppAuthState>();
builder.Services.AddScoped<IV.ManagementHub.Web.Services.ConsoleLogService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseDXWebApiCorrelationId();
app.UseDXWebApiSecurityHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.UseDXWebApiExecutionContext();
app.UseDXWebApiRateLimiting();

// Per-process proxy instance registry (populated lazily on first proxy request)
var proxyRegistry = new ConcurrentDictionary<string, (string BaseUrl, string ServiceKey)>(
    StringComparer.OrdinalIgnoreCase);

// --- Proxy middleware: routes /api/{typeName} to the selected DX instance ---
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (!path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    // Skip DX-native and auth routes
    if (path.StartsWithSegments("/api/auth") ||
        path.StartsWithSegments("/api/management") ||
        path.StartsWithSegments("/api/service-auth"))
    {
        await next();
        return;
    }

    // Proxy routes require X-MH-Instance header
    var instanceKey = context.Request.Headers["X-MH-Instance"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(instanceKey))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "Missing instance key. Send 'X-MH-Instance' header." });
        return;
    }

    if (!proxyRegistry.TryGetValue(instanceKey, out var entry))
    {
        // Load instances directly from MH's own DX data layer (in-process, handles encrypted columns)
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MH.ProxyMiddleware");
        try
        {
            var dataReader = context.RequestServices.GetRequiredService<IDXUnitDataReader>();
            var instances = await dataReader.GetItemsAsync<MHInstanceUnit>(ct: context.RequestAborted);
            foreach (var unit in instances.Where(u => !string.IsNullOrWhiteSpace(u.Key)
                && !string.IsNullOrWhiteSpace(u.BaseUrl)
                && !string.IsNullOrWhiteSpace(u.ServiceKey)))
            {
                proxyRegistry[unit.Key] = (unit.BaseUrl, unit.ServiceKey);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Proxy registry load threw an exception.");
        }

        if (!proxyRegistry.TryGetValue(instanceKey, out entry))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = $"Instance '{instanceKey}' was not found." });
            return;
        }
    }

    InstanceApiContext.Set(entry.BaseUrl, entry.ServiceKey);
    await next();
});

app.UseAntiforgery();
app.UseOutputCache();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapControllers();

app.Run();
