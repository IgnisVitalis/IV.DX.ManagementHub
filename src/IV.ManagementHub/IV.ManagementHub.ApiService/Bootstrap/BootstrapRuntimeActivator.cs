using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Hosting;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapRuntimeActivator(
        IServiceProvider rootServiceProvider,
        BootstrapRuntimeState runtimeState,
        ILogger<BootstrapRuntimeActivator> logger) : IBootstrapRuntimeActivator
    {
        private readonly SemaphoreSlim _sync = new(1, 1);

        public async Task<BootstrapActivationResult> ActivateAsync(CancellationToken ct = default)
        {
            if (runtimeState.IsDxRuntimeEnabled)
            {
                return new BootstrapActivationResult(true, "DX runtime is already active.");
            }

            await _sync.WaitAsync(ct);
            try
            {
                if (runtimeState.IsDxRuntimeEnabled)
                {
                    return new BootstrapActivationResult(true, "DX runtime is already active.");
                }

                if (!runtimeState.IsHandlersInitialized)
                {
                    rootServiceProvider.InitializeDXHandlers();
                    runtimeState.MarkHandlersInitialized();
                }

                using var scope = rootServiceProvider.CreateScope();
                var init = scope.ServiceProvider.GetRequiredService<IDXInitializer>();
                await init.InitDXCoreDataAsync(ct);
                await init.InitDXQueryDataAsync(ct);
                await init.InitDXSecurityDataAsync(ct);
                await init.InitCustomDataAsync("Migration/MH.json", ct);

                runtimeState.MarkRuntimeEnabled();
                return new BootstrapActivationResult(true, "DX runtime activated.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DX runtime activation failed.");
                return new BootstrapActivationResult(false, ex.Message);
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}
