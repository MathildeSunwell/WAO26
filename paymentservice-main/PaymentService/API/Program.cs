using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentService.API.Extensions;
using PaymentService.Infrastructure.Database;
using PaymentService.Infrastructure.Messaging;

var apiFolder = Path.Combine(Directory.GetCurrentDirectory(), "API");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = apiFolder,
    WebRootPath = Path.Combine(apiFolder, "wwwroot"),
    Args = args
});

builder.Services.AddRabbitMq(builder.Configuration); 
builder.Services.AddServices(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCustomHealthChecks(builder.Configuration);
builder.AddSerilogLogging();
builder.Services.AddInstrumentation("PaymentService");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.UseCustomHealthChecks();
app.UseRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public abstract partial class Program { }
