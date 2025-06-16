using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentService.API.Extensions;
using PaymentService.Infrastructure.Database;
using PaymentService.Infrastructure.Messaging;

// Configure application paths for folder structure
var apiFolder = Path.Combine(Directory.GetCurrentDirectory(), "API");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = apiFolder,          
    WebRootPath = Path.Combine(apiFolder, "wwwroot"),  
    Args = args                           
});

// Register custom services using extension methods
// These are defined in ServiceCollectionExtensions.cs
builder.Services.AddRabbitMq(builder.Configuration);    // RabbitMQ connection & topology
builder.Services.AddServices(builder.Configuration);    // Business services & repositories

// Add standard ASP.NET Core services
builder.Services.AddControllers();          
builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen();           

// Add observability and monitoring services
builder.Services.AddCustomHealthChecks(builder.Configuration);  // SQL & RabbitMQ health checks
builder.AddSerilogLogging();                                    
builder.Services.AddInstrumentation("PaymentService");          // OpenTelemetry distributed tracing

// Build the application with all registered services
var app = builder.Build();

// Configure middleware pipeline for DEVELOPMENT only
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();    
    app.UseSwaggerUI();  
}

// Run database migrations on startup
// This ensures the database is always up-to-date when the service starts
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();  // Apply any pending EF Core migrations
}

// Configure middleware pipeline
app.UseCustomHealthChecks();  
app.UseRequestLogging();      

app.UseHttpsRedirection();    
app.UseAuthorization();       

app.MapControllers();         

app.Run();

public abstract partial class Program { }

/*
 * What this file does:
 * 
 * 1. BOOTSTRAP: Sets up the entire payment service application
 * 2. DEPENDENCY INJECTION: Registers all services (messaging, database, logging, etc.)
 * 3. MIDDLEWARE PIPELINE: Configures request/response processing chain
 * 4. DATABASE: Automatically applies migrations on startup
 * 5. OBSERVABILITY: Sets up health checks, logging, and tracing
 * 6. API: Enables controllers and Swagger documentation
 * 
 */