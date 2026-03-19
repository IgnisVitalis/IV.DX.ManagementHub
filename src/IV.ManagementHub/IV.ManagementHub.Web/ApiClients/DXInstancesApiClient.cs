using IV.ManagementHub.ApiService.Bootstrap;

namespace IV.ManagementHub.Web.ApiClients
{
    internal sealed class DXInstancesApiClient(IBootstrapInstanceService instanceService)
    {
        public async Task<IReadOnlyList<DXInstanceDto>> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            var instances = await instanceService.GetInstancesAsync(cancellationToken);
            return instances
                .Select(i => new DXInstanceDto
                {
                    Key = i.Key,
                    Title = i.Title,
                    ApiUrl = i.ApiUrl,
                    CreatedAtUtc = i.CreatedAtUtc
                })
                .ToList();
        }

        public async Task<CreateInstanceResult> CreateAsync(CreateInstanceRequest request, CancellationToken cancellationToken = default)
        {
            var result = await instanceService.CreateInstanceAsync(
                new BootstrapCreateInstanceRequest(
                    request.Key,
                    request.Title,
                    request.ApiUrl,
                    request.ServiceKey),
                cancellationToken);

            if (result.Status == BootstrapCreateInstanceStatus.Created && result.Instance is not null)
            {
                var dto = new DXInstanceDto
                {
                    Key = result.Instance.Key,
                    Title = result.Instance.Title,
                    ApiUrl = result.Instance.ApiUrl,
                    CreatedAtUtc = result.Instance.CreatedAtUtc
                };
                return CreateInstanceResult.Success(dto, result.Message ?? "Instance created.");
            }

            return CreateInstanceResult.Fail(result.Message ?? "Failed to create instance.");
        }
    }

    internal sealed class DXInstanceDto
    {
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string ApiUrl { get; init; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; init; }
    }

    internal sealed record CreateInstanceRequest(
        string Key,
        string Title,
        string ApiUrl,
        string ServiceKey);

    internal sealed record CreateInstanceResult(bool IsSuccess, DXInstanceDto? Instance, string? Message, string? Error)
    {
        public static CreateInstanceResult Success(DXInstanceDto instance, string message) =>
            new(true, instance, message, null);

        public static CreateInstanceResult Fail(string error) =>
            new(false, null, null, error);
    }
}
