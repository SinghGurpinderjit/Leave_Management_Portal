namespace Shared.Common.Configuration;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string QueueName { get; set; } = string.Empty; 
    public string ExchangeName { get; set; } = "microservices.exchange";
    public string RoutingKey { get; set; } = string.Empty; 
    public int MessageTtlMilliseconds { get; set; } = 86400000; // 24 hrs
}

public class UserCreatedConsumerSettings : RabbitMQSettings { }

public class LeaveStatusUpdatedConsumerSettings : RabbitMQSettings { }

public class LeaveStatusPublishSettings: RabbitMQSettings { }
