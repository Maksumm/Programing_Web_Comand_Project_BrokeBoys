using ApiApp.Common;
using ApiApp.Data;
using ApiApp.Features.Categories;
using ApiApp.Features.Me;
using ApiApp.Features.Prices;
using ApiApp.Features.Products;
using ApiApp.Features.Subscriptions;
using Microsoft.EntityFrameworkCore;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEntraAuthentication();
builder.Services.AddOpenApi();

// PostgreSQL через EF Core. Рядок підключення — ConnectionStrings:Catalogue (user-secrets / змінні середовища),
// читається при створенні контексту, а не під час запуску.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(services.GetRequiredService<IConfiguration>().GetConnectionString("Catalogue")));

// CORS лише для адреси клієнта; заголовок Location відкриваємо, щоб клієнт міг його прочитати після POST.
var clientOrigin = builder.Configuration["Cors:ClientOrigin"]
    ?? throw new InvalidOperationException("Cors:ClientOrigin is not configured.");
builder.Services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
    .WithOrigins(clientOrigin)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location")));

var app = builder.Build();

// Демо-дані лише для розробки (Seed:Demo = true в appsettings.Development.json). Схему створюють міграції, не застосунок.
if (app.Configuration.GetValue<bool>("Seed:Demo"))
{
    using var scope = app.Services.CreateScope();
    try
    {
        await DemoData.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Demo data was not seeded. Is PostgreSQL running and are the migrations applied (dotnet ef database update)?");
    }
}

// Першим, щоб бачити остаточний код відповіді (разом з обробленими помилками).
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json лише в Development
}

app.UseHttpsRedirection();
app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapMe();
app.MapCategories();
app.MapProducts();
app.MapPrices();
app.MapSubscriptions();

app.Run();

// Потрібен для WebApplicationFactory<Program> в інтеграційних тестах.
public partial class Program { }
