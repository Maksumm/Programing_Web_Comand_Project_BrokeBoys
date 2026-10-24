using ApiApp.Common;
using ApiApp.Data;
using ApiApp.Features.Categories;
using ApiApp.Features.Prices;
using ApiApp.Features.Products;
using ApiApp.Features.Subscriptions;

const string ClientCorsPolicy = "Client";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Дані зберігаються в пам'яті процесу (робота 3); один екземпляр на весь застосунок.
builder.Services.AddSingleton(_ => CatalogueStore.CreateSeeded());

// CORS лише для адреси клієнта; заголовок Location відкриваємо, щоб клієнт міг його прочитати після POST.
var clientOrigin = builder.Configuration["Cors:ClientOrigin"]
    ?? throw new InvalidOperationException("Cors:ClientOrigin is not configured.");
builder.Services.AddCors(options => options.AddPolicy(ClientCorsPolicy, policy => policy
    .WithOrigins(clientOrigin)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("Location")));

var app = builder.Build();

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

app.MapCategories();
app.MapProducts();
app.MapPrices();
app.MapSubscriptions();

app.Run();

// Потрібен для WebApplicationFactory<Program> в інтеграційних тестах.
public partial class Program { }
