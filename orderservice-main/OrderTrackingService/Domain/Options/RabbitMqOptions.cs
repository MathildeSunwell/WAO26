namespace OrderTrackingService.Domain.Options;

public class RabbitMqOptions
{
    public string SectionName { get; } = "RabbitMq";
    public string HostName    { get; set; }
    public int    Port        { get; set; }
    public string VirtualHost { get; set; }
    public string UserName    { get; set; }
    public string Password    { get; set; }
}