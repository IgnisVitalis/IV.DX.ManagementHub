using System.Collections.Concurrent;
using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Hosting;
using IV.DX.Kernel.Models;
using IV.DX.PostgreSQL;
using IV.DX.Presentation.Hosting;
using IV.DX.WebApi.Auth.DependencyInjection;
using IV.DX.WebApi.DependencyInjection;
using IV.DX.WebApi.Management.DependencyInjection;
using IV.DX.ManagementHub.ApiService.Bootstrap;
using IV.DX.ManagementHub.ApiService.Services;
using IV.DX.ManagementHub.Common.Models;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// --- DX Core ---
builder.Services
    .AddDX(builder.Configuration)
    .UsePostgreSQL()
    .AddSecurity()
    .AddActions(typeof(Program).Assembly)
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
    .AddDXManagementControllers();          // api/management/* (DX CRUD, adds Newtonsoft)

// --- DX Rate limiting ---
builder.Services.AddDXWebApiRateLimiting(builder.Configuration);

// --- Host services ---
builder.Services.AddHttpClient();

// In Development, the ASP.NET Core dev certificate isn't in the OpenSSL trust
// store on Linux, so server-side self-calls to https://localhost fail the TLS
// handshake. Accept the untrusted cert for loopback only; remote instances are
// still fully validated.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient(string.Empty)
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                errors == System.Net.Security.SslPolicyErrors.None ||
                request.RequestUri?.IsLoopback == true,
        });
}

builder.Services.AddSingleton<InstanceApiClientFactory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseDXWebApiCorrelationId();
app.UseDXWebApiSecurityHeaders();

// --- Instance routing, part 1: path ---
//
// Instance-scoped API calls carry the instance in the path: `/api/i/{key}/...`.
// The key is taken out of the path here and the request continues as
// `/api/...`, so the ApiService controllers keep their plain routes
// (`api/{typeName}`, `api/DXQueryResult`, …).
//
// The instance lives in the path rather than in a header on purpose: HTTP caches
// key on the URL, so a header would let one instance's response be served for
// another unless every layer sets `Vary` correctly.
//
// This has to run before routing: `WebApplication` inserts `UseRouting` at the
// start of the pipeline by default, and an endpoint chosen from the original
// path would ignore any later rewrite. Calling `UseRouting` explicitly below
// suppresses that automatic insertion.
const string instancePrefix = "/api/i/";
const string instanceKeyItem = "MH.InstanceKey";

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;

    if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    // Served by this host itself and never instance-scoped: DX management
    // (local data), DX auth, and service token issuing.
    if (context.Request.Path.StartsWithSegments("/api/auth") ||
        context.Request.Path.StartsWithSegments("/api/management") ||
        context.Request.Path.StartsWithSegments("/api/service-auth"))
    {
        await next();
        return;
    }

    if (!path.StartsWith(instancePrefix, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Instance-scoped API requires the '/api/i/{instanceKey}/...' form."
        });
        return;
    }

    var rest = path[instancePrefix.Length..];
    var separator = rest.IndexOf('/');
    var instanceKey = separator < 0 ? rest : rest[..separator];
    var tail = separator < 0 ? string.Empty : rest[separator..];

    if (string.IsNullOrWhiteSpace(instanceKey))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "Instance key is missing from the path." });
        return;
    }

    context.Items[instanceKeyItem] = instanceKey;
    context.Request.Path = "/api" + tail;

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseDXWebApiExecutionContext();
app.UseDXWebApiRateLimiting();

// Per-process instance registry (populated lazily on first instance-scoped request)
var instanceRegistry = new ConcurrentDictionary<string, (string BaseUrl, string ServiceKey)>(
    StringComparer.OrdinalIgnoreCase);

// --- Instance routing, part 2: connection ---
//
// Resolving the key touches the database, so it happens after authorization:
// an anonymous caller must not be able to probe which instance keys exist.
app.Use(async (context, next) =>
{
    if (context.Items[instanceKeyItem] is not string instanceKey)
    {
        await next();
        return;
    }

    if (!instanceRegistry.TryGetValue(instanceKey, out var entry))
    {
        // Load instances from MH's own DX data layer (in-process, handles encrypted columns)
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("MH.InstanceRouting");
        try
        {
            var dataReader = context.RequestServices.GetRequiredService<IDXUnitDataReader>();
            var instances = await dataReader.GetItemsAsync<MHInstanceUnit>(ct: context.RequestAborted);
            foreach (var unit in instances.Where(u => !string.IsNullOrWhiteSpace(u.Key)
                && !string.IsNullOrWhiteSpace(u.BaseUrl)
                && !string.IsNullOrWhiteSpace(u.ServiceKey)))
            {
                instanceRegistry[unit.Key] = (unit.BaseUrl, unit.ServiceKey);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Instance registry load threw an exception.");
        }

        if (!instanceRegistry.TryGetValue(instanceKey, out entry))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = $"Instance '{instanceKey}' was not found." });
            return;
        }
    }

    InstanceApiContext.Set(entry.BaseUrl, entry.ServiceKey);

    await next();
});

app.MapControllers();

app.Run();
