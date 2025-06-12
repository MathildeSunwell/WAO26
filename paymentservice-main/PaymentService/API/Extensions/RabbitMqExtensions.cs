using PaymentService.Domain;
using PaymentService.Domain.Options;
using RabbitMQ.Client;

namespace PaymentService.API.Extensions;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ConnectionFactory>>();
            var settings = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>();
            
            if (settings == null)
            {
                throw new InvalidOperationException("RabbitMQ configuration is missing");
            }
            
            var factory = new ConnectionFactory
            {
                HostName   = settings.HostName,
                UserName   = settings.UserName,
                Password   = settings.Password,
                Port       = settings.Port
            };
            logger.LogInformation("RabbitMQ config → Host: {Host}, Port: {Port}, User: {User}", factory.HostName, factory.Port, factory.UserName);
            return factory;
        });

        services.AddSingleton<IConnection>(serviceProvider =>
        {
            var factory = serviceProvider.GetRequiredService<ConnectionFactory>();
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IChannel>(sp =>
        {
            var connection = sp.GetRequiredService<IConnection>();
            var consumeChannel = connection.CreateChannelAsync().GetAwaiter().GetResult();

            ConfigureTopologyAsync(consumeChannel).GetAwaiter().GetResult();

            return consumeChannel;
        });

        return services;
    }
    
    private static async Task ConfigureTopologyAsync(IChannel channel)
    {
        // Declare exchanges
        await channel.ExchangeDeclareAsync(
            exchange:   RabbitMqTopology.EventExchange,
            type:       ExchangeType.Topic,
            durable:    true,
            autoDelete: false,
            arguments:  null
        );
        
        // Event queue
        await channel.QueueDeclareAsync(
            queue:      RabbitMqTopology.EventQueue,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = RabbitMqTopology.RetryQueue
            });
        
        foreach (var key in RabbitMqTopology.RoutingKeys)
        {
            await channel.QueueBindAsync(
                queue: RabbitMqTopology.EventQueue,
                exchange: RabbitMqTopology.EventExchange,
                routingKey: key
            );
        }
        
        // Retry queue
        await channel.QueueDeclareAsync(
            queue:      RabbitMqTopology.RetryQueue,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  new Dictionary<string, object?>
            {
                ["x-message-ttl"]             = 30_000, // 30s
                ["x-dead-letter-exchange"]    = "",
                ["x-dead-letter-routing-key"] = RabbitMqTopology.EventQueue
            }
        );
        
        // Dead letter queue
        await channel.QueueDeclareAsync(
            queue:      RabbitMqTopology.DeadLetterQueue,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  null
        );
        
        // Configure queue prefetch to avoid overwhelming the service
        await channel.BasicQosAsync(0, 1, false);
    }
}