using IV.DX.Hosting;
using System.Reflection;

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
                    // Refresh structure cache — may be empty after app restart since StartDXAsync is skipped.
                    // runtimeBinder.Bind already set the correct connection string, so this reads the right DB.
                    await RefreshStructureCacheAsync(rootServiceProvider, ct);
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
                    await RefreshStructureCacheAsync(rootServiceProvider, ct);
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

                await rootServiceProvider.StartDXAsync(ct);

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

        // IDXStructureCache is internal in IV.DX — resolve and call via reflection.
        private static async Task RefreshStructureCacheAsync(IServiceProvider serviceProvider, CancellationToken ct)
        {
            var cacheType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .FirstOrDefault(t => t.IsInterface && t.Name == "IDXStructureCache");

            if (cacheType is null) return;

            var cache = serviceProvider.GetService(cacheType);
            if (cache is null) return;

            var refreshMethod = cacheType.GetMethod("RefreshAsync");
            if (refreshMethod is null) return;

            var result = refreshMethod.Invoke(cache, new object[] { ct });
            if (result is Task task)
                await task;
        }
    }
}
