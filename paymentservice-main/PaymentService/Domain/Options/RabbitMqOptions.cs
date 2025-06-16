namespace PaymentService.Domain.Options;

public class RabbitMqOptions
{
    public string SectionName { get; } = "RabbitMq";
    public string HostName { get; set; }
    public int Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}


/*
    RabbitMqOptions is a configuration class used to bind RabbitMQ settings
    from the appsettings.json file (or any other configuration provider).

    It defines properties such as:
    - HostName: the RabbitMQ server address
    - Port: the port used for AMQP (typically 5672)
    - UserName and Password: credentials for authentication

    The SectionName property ("RabbitMq") is used to locate the corresponding
    configuration section.

    This class allows dependency-injected services to easily access typed RabbitMQ
    connection settings without hardcoding values.
*/
