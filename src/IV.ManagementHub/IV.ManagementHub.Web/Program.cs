using IV.DataProvider.WebApp.Services.Web.Contracts;
using IV.DataProvider.WebApp.Services.Web.Services;
using IV.ManagementHub.Web;
using IV.ManagementHub.Web.Services;
using IV.ManagementHub.Web.Components;
using Microsoft.FluentUI.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();

builder.Services.AddOutputCache();

var configuredSources = builder.Configuration.GetSection("ApiSources").Get<List<ApiSourceDefinition>>() ?? new List<ApiSourceDefinition>();
var apiSourceCatalog = new ApiSourceCatalog(configuredSources);
builder.Services.AddSingleton(apiSourceCatalog);

foreach (var source in apiSourceCatalog.Sources)
{
    builder.Services.AddHttpClient(source.HttpClientName, http => http.BaseAddress = new Uri(source.ApiBaseUrl));
}

builder.Services.AddScoped<IApiClientResolver, ApiClientResolver>();
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<AppAuthState>();
builder.Services.AddScoped<RootAuthApiClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
