namespace LeaveManagementService.Models
{
    public class ServiceDiscoverySettings
    {
        public bool UseEureka { get; set; }
        public UserServiceSettings UserService { get; set; } = new();
    }

    public class UserServiceSettings
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DirectUrl { get; set; } = string.Empty;
    }

}
