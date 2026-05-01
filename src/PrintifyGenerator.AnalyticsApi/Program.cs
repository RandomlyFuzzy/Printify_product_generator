using PrintifyGenerator.AnalyticsApi.Models;
using PrintifyGenerator.AnalyticsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AnalyticsApiOptions>(builder.Configuration.GetSection("AnalyticsApi"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<PhaseDataService>();
builder.Services.AddSingleton<MarketFeatureService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Only redirect to HTTPS when a proper HTTPS port is configured
if (!string.IsNullOrEmpty(builder.Configuration["ASPNETCORE_HTTPS_PORT"])
    || !string.IsNullOrEmpty(builder.Configuration["https_port"]))
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => Results.Ok(new
{
    service = "PrintifyGenerator.AnalyticsApi",
    status = "ok",
    docs = "/openapi/v1.json"
}));

app.MapControllers();

app.Run();
