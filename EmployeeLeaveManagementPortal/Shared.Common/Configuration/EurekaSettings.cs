namespace Shared.Common.Configuration;

public class EurekaSettings
{
    public string ServerUrl { get; set; } = "http://localhost:8761/eureka";
    public string AppName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; }
    public bool PreferIpAddress { get; set; } = false;
}
