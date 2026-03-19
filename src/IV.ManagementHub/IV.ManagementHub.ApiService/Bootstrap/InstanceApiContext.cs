namespace IV.ManagementHub.ApiService.Bootstrap
{
    /// <summary>
    /// Per-request (AsyncLocal) API connection context.
    /// Each async request flow gets its own copy so concurrent requests
    /// for different instances do not interfere with each other.
    /// </summary>
    internal static class InstanceApiContext
    {
        private static readonly AsyncLocal<string?> _apiUrl = new();
        private static readonly AsyncLocal<string?> _serviceKey = new();

        public static string? ApiUrl => _apiUrl.Value;
        public static string? ServiceKey => _serviceKey.Value;

        public static void Set(BootstrapInstanceSettings instance)
        {
            _apiUrl.Value = instance.ApiUrl;
            _serviceKey.Value = instance.ServiceKey;
        }
    }
}
