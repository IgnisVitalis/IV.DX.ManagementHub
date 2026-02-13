using Asp.Versioning;
using IV.DX.Hosting;
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

var rootAuthOptions = builder.Configuration.GetSection(RootAuthOptions.SectionName).Get<RootAuthOptions>() ?? new RootAuthOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rootAuthOptions.SigningKey));

builder.Services.AddSingleton(rootAuthOptions);
builder.Services.AddSingleton<RootTokenService>();

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

builder.Configuration["Database:Type"] = "PostgreSQL";
builder.Configuration["Database:ConnectionString"] = "Server=localhost;Database=IV.ManagementHub;User ID=postgres;password=root;";

builder.Services.AddDXCore(builder.Configuration);
builder.Services.AddDXPipeline();
builder.Services.AddDXInitializer();

builder.Services.AddScoped<IDXUnitStructureService, DXUnitStructureService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Services.InitializeDXHandlers();


using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<IDXInitializer>();
    await init.InitDXCoreDataAsync();
    await init.InitDXQueryDataAsync();
    await init.InitDXSecurityDataAsync();
    await init.InitCustomDataAsync("Migration/MH.json");
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
