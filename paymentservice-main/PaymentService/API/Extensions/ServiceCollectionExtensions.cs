using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Services;
using PaymentService.Infrastructure.Database;
using PaymentService.Infrastructure.Messaging;

namespace PaymentService.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Logging
        services.AddLogging();

        // Database
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), opts => 
                opts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(2), errorNumbersToAdd: null)));

        // RabbitMQ
        services.AddRabbitMq(configuration);
        services.AddScoped<IMessagePublisher, RabbitMqMessagePublisher>();
        services.AddScoped<IPaymentEventPublisher, RabbitMqPaymentEventPublisher>();
        services.AddHostedService<RabbitMqPaymentEventConsumer>();

        // Services
        services.AddScoped<IPaymentService, PaymentProcessorService>();

        // Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        // Options
        services.AddJsonOptions();

        return services;
    }

    private static void AddJsonOptions(this IServiceCollection services)
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        services.AddSingleton(jsonOptions);
    }
}