using Microsoft.Extensions.FileProviders;
using PrintifyGenerator.Dashboard.Models;
using PrintifyGenerator.Dashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<DashboardOptions>(builder.Configuration.GetSection(DashboardOptions.SectionName));
builder.Services.AddSingleton<DashboardDataService>();
builder.Services.AddSingleton<BlueprintCatalogService>();
builder.Services.AddSingleton<GenerationRuntimeService>();
builder.Services.AddSingleton<IHostedService>(static serviceProvider => serviceProvider.GetRequiredService<GenerationRuntimeService>());
builder.Services.AddSingleton<SwipeReviewMoveQueueService>();
builder.Services.AddSingleton<IHostedService>(static serviceProvider => serviceProvider.GetRequiredService<SwipeReviewMoveQueueService>());
builder.Services.AddHttpClient<StagingSwipeReviewService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<NodeHealthService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

var dataRoot = DashboardOptions.ResolveDataRoot(
    builder.Configuration.GetSection(DashboardOptions.SectionName)[nameof(DashboardOptions.DataRoot)],
    app.Environment.ContentRootPath);

var checkingRoot = Path.Combine(dataRoot, "Checking");
if (Directory.Exists(checkingRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(checkingRoot),
        RequestPath = "/generated"
    });
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
