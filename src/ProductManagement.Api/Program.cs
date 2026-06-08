using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProductManagement.Api.Common;
using ProductManagement.Api.Endpoints;
using ProductManagement.Application;
using ProductManagement.Application.Abstractions;
using ProductManagement.Infrastructure;
using ProductManagement.Infrastructure.Persistence;
using ProductManagement.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

// Composition root.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// RFC 7807 problem details + central exception handling.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Product Management API", Version = "v1" }));

var app = builder.Build();

app.UseExceptionHandler();

// One-command run: apply migrations and seed (idempotent). Toggle via Database:AutoMigrate.
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).WithTags("Health");

app.MapProductEndpoints();
app.MapVariantEndpoints();
app.MapCategoryEndpoints();

app.Run();

// Exposed so the integration test project can spin up the API via WebApplicationFactory.
public partial class Program;
