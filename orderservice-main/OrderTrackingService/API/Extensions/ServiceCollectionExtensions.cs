using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OrderTrackingService.Application;
using OrderTrackingService.Infrastructure.Database;
using OrderTrackingService.Infrastructure.Messaging;

namespace OrderTrackingService.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Logging
        services.AddLogging();
        
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), opts => 
                opts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null)));
        
        // RabbitMQ
        services.AddRabbitMq(configuration);
        services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
        services.AddScoped<IOrderEventPublisher, RabbitMqOrderEventPublisher>();
        services.AddHostedService<OrderEventConsumer>();
        
        // Services
        services.AddScoped<IOrderService, Application.OrderService>();
        
        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        
        // Options
        services.AddJsonOptions();
        
        return services;
    }
    
    private static void AddJsonOptions(this IServiceCollection services)
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        services.AddSingleton(jsonOptions);
    }
}