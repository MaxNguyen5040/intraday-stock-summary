using StockSummary.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MarketDataExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddOptions<RateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddApiRateLimiting();

var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new();
builder.Services.AddCors(cors => cors.AddPolicy(CorsSettings.PolicyName, policy => policy
    .WithOrigins([.. corsSettings.AllowedOrigins])
    .WithMethods(HttpMethods.Get)
    .AllowAnyHeader()));

builder.Services.AddStockSummary(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsSettings.PolicyName);
app.UseRateLimiter();

app.MapHealthChecks("/healthz");
app.MapControllers();

app.Run();
