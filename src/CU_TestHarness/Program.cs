using CU_TestHarness.Components;
using CU_TestHarness.Models;
using CU_TestHarness.Services;

var builder = WebApplication.CreateBuilder(args);

// Load optional config overlay (e.g. appsettings.Personal.json via CU_CONFIG_OVERLAY env var)
var overlay = Environment.GetEnvironmentVariable("CU_CONFIG_OVERLAY");
if (!string.IsNullOrEmpty(overlay))
{
    builder.Configuration.AddJsonFile($"appsettings.{overlay}.json", optional: true, reloadOnChange: true);
}

// Bind configuration
builder.Services.Configure<ContentUnderstandingOptions>(
    builder.Configuration.GetSection("ContentUnderstanding"));
builder.Services.Configure<DocumentIntelligenceOptions>(
    builder.Configuration.GetSection("DocumentIntelligence"));

// Register services
builder.Services.AddSingleton<CostEstimator>();
builder.Services.AddSingleton(new ModelProfileState(
    builder.Configuration["ModelDeployments:DefaultCompletion"] ?? "gpt41-mini-standard",
    builder.Configuration["ModelDeployments:DefaultEmbedding"] ?? "ada-002"));
builder.Services.AddHttpClient<ContentUnderstandingService>();
builder.Services.AddHttpClient<DocumentIntelligenceService>();
builder.Services.AddSingleton<CompletionService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
