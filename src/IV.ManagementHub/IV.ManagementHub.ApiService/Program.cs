using Asp.Versioning;
using IV.DX.Hosting;
using IV.ManagementHub.ApiService.Contracts.Services;
using IV.ManagementHub.ApiService.Services;
using Newtonsoft.Json.Serialization;

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


builder.Configuration["Database:Type"] = "PostgreSQL";
builder.Configuration["Database:ConnectionString"] = "Server=localhost;Database=IV.ManagementHub;User ID=postgres;password=root;";

builder.Services.AddDXCore(builder.Configuration);
builder.Services.AddDXPipeline();
builder.Services.AddDXInitializer();

builder.Services.AddScoped<IDXUnitStructureService, DXUnitStructureService>();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Services.InitializeDXHandlers();


using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<IDXInitializer>();
    await init.InitCoreDataAsync();
    await init.InitCustomDataAsync("Migration/MH.json");
}

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();