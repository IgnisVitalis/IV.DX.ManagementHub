using IV.DX.Application.Contracts.Abstractions;
using IV.DX.Hosting;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapRuntimeActivator(
        IServiceProvider rootServiceProvider,
        BootstrapRuntimeState runtimeState,
        BootstrapSettingsSnapshot settingsSnapshot,
        IDatabaseRuntimeBinder runtimeBinder,
        ILogger<BootstrapRuntimeActivator> logger) : IBootstrapRuntimeActivator
    {
        private readonly SemaphoreSlim _sync = new(1, 1);

        public async Task<BootstrapActivationResult> ActivateAsync(string instanceKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(instanceKey))
            {
                return new BootstrapActivationResult(false, "Instance key is required.");
            }

            var normalizedInstanceKey = instanceKey.Trim();

            var settings = settingsSnapshot.Current;
            var instance = settings?.ResolveInstance(normalizedInstanceKey);
            if (instance is null)
            {
                return new BootstrapActivationResult(false, $"Instance '{normalizedInstanceKey}' was not found.");
            }

            await _sync.WaitAsync(ct);
            try
            {
                var bindingResult = runtimeBinder.Bind(instance);
                if (!bindingResult.IsSuccess)
                {
                    return new BootstrapActivationResult(false, bindingResult.Message);
                }

                ReinitializeDxHandlers(rootServiceProvider);
                runtimeState.MarkHandlersInitialized();

                if (instance.IsInitialized == true)
                {
                    runtimeState.MarkDatabaseActivated(instance.DatabaseType, instance.ConnectionString);
                    runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                    return new BootstrapActivationResult(true, $"DX runtime is already active for instance '{normalizedInstanceKey}'.");
                }

                if (runtimeState.IsDatabaseActivated(instance.DatabaseType, instance.ConnectionString))
                {
                    runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                    return new BootstrapActivationResult(true, $"DX runtime is already active for instance '{normalizedInstanceKey}'.");
                }

                var hasPersistedInitializedEquivalentInstance = settings?.Instances.Any(candidate =>
                    !string.Equals(candidate.Key, normalizedInstanceKey, StringComparison.OrdinalIgnoreCase) &&
                    candidate.IsInitialized != false &&
                    BootstrapDatabaseIdentity.AreEquivalent(
                        candidate.DatabaseType,
                        candidate.ConnectionString,
                        instance.DatabaseType,
                        instance.ConnectionString)) == true;

                if (hasPersistedInitializedEquivalentInstance)
                {
                    runtimeState.MarkDatabaseActivated(instance.DatabaseType, instance.ConnectionString);
                    runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                    return new BootstrapActivationResult(true, $"DX runtime is already active for instance '{normalizedInstanceKey}'.");
                }

                var hasActivatedEquivalentInstance = settings?.Instances.Any(candidate =>
                    runtimeState.IsInstanceActivated(candidate.Key) &&
                    BootstrapDatabaseIdentity.AreEquivalent(
                        candidate.DatabaseType,
                        candidate.ConnectionString,
                        instance.DatabaseType,
                        instance.ConnectionString)) == true;

                if (hasActivatedEquivalentInstance)
                {
                    runtimeState.MarkDatabaseActivated(instance.DatabaseType, instance.ConnectionString);
                    runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                    return new BootstrapActivationResult(true, $"DX runtime is already active for instance '{normalizedInstanceKey}'.");
                }

                if (BootstrapDatabaseProbe.HasDxCoreSignature(instance))
                {
                    runtimeState.MarkDatabaseActivated(instance.DatabaseType, instance.ConnectionString);
                    runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                    return new BootstrapActivationResult(true, $"DX runtime is already active for instance '{normalizedInstanceKey}'.");
                }

                using var scope = rootServiceProvider.CreateScope();
                var init = scope.ServiceProvider.GetRequiredService<IDXInitializer>();
                await init.InitDXCoreDataAsync(ct);
                await init.InitDXQueryDataAsync(ct);
                await init.InitDXSecurityDataAsync(ct);
                await init.InitCustomDataAsync("Migration/MH.json", ct);

                runtimeState.MarkDatabaseActivated(instance.DatabaseType, instance.ConnectionString);
                runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                return new BootstrapActivationResult(true, $"DX runtime activated for instance '{normalizedInstanceKey}'.");
            }
            catch (Exception ex)
            {
                if (IsAlreadyInitializedDatabaseError(ex))
                {
                    runtimeState.MarkDatabaseActivated(instance.DatabaseType, instance.ConnectionString);
                    runtimeState.MarkInstanceActivated(normalizedInstanceKey);
                    logger.LogWarning(ex, "DX runtime activation detected existing schema. Activation marked as successful.");
                    return new BootstrapActivationResult(true, $"DX runtime is already active for instance '{normalizedInstanceKey}'.");
                }

                logger.LogError(ex, "DX runtime activation failed.");
                return new BootstrapActivationResult(false, ex.Message);
            }
            finally
            {
                _sync.Release();
            }
        }

        private static bool IsAlreadyInitializedDatabaseError(Exception ex)
        {
            foreach (var message in EnumerateExceptionMessages(ex))
            {
                if (message.Contains("42P07", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (message.Contains("UC_DXElementInUnitTypeEnum_Key", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> EnumerateExceptionMessages(Exception ex)
        {
            if (ex is null)
            {
                yield break;
            }

            yield return ex.Message ?? string.Empty;

            if (ex is AggregateException aggregateException)
            {
                foreach (var inner in aggregateException.InnerExceptions)
                {
                    foreach (var message in EnumerateExceptionMessages(inner))
                    {
                        yield return message;
                    }
                }
            }

            if (ex.InnerException is not null)
            {
                foreach (var message in EnumerateExceptionMessages(ex.InnerException))
                {
                    yield return message;
                }
            }
        }

        private static void ReinitializeDxHandlers(IServiceProvider rootServiceProvider)
        {
            rootServiceProvider.InitializeDXHandlers();
        }
    }
}
