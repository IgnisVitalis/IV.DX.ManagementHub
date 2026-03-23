using IV.DX.Hosting;
using Microsoft.Extensions.Hosting;

namespace IV.ManagementHub.Web.Services;

// Runs MH custom data migrations after DX core and DX Presentation have fully initialized.
// Must be registered after both AddDX(...).RegisterHostedService() and AddDXPresentation().RegisterHostedService().
internal sealed class MHCustomDataHostedService(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => serviceProvider.InitCustomEmbeddedDataAsync(
            typeof(MHCustomDataHostedService).Assembly,
            "MigrationScripts/ManagementHub.json",
            cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
