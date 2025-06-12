using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderTrackingService.API.Extensions;
using OrderTrackingService.Infrastructure.Database;

var apiFolder = Path.Combine(Directory.GetCurrentDirectory(), "API");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = apiFolder,
    WebRootPath     = Path.Combine(apiFolder, "wwwroot"),
    Args            = args,
});

Activity.DefaultIdFormat = ActivityIdFormat.W3C;

builder.Services.AddServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.AddSerilogLogging();
builder.Services.AddInstrumentation("OrderTrackingService");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Ensure the database is created and apply any pending migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseCustomHealthChecks();
app.UseRequestLogging();
app.UseHttpServerTracing();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public abstract partial class Program { }