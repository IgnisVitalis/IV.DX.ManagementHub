using Microsoft.Extensions.Options;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    public interface IDatabaseRuntimeBinder
    {
        BootstrapBindingResult Bind(BootstrapInstanceSettings instance);
    }

    public sealed record BootstrapBindingResult(bool IsSuccess, string? Message = null);

    public sealed class DatabaseRuntimeBinder(
        IConfiguration configuration,
        IServiceProvider rootServiceProvider) : IDatabaseRuntimeBinder
    {
        private static readonly object Sync = new();

        public BootstrapBindingResult Bind(BootstrapInstanceSettings instance)
        {
            if (instance is null)
            {
                return new BootstrapBindingResult(false, "Instance payload is required.");
            }

            if (string.IsNullOrWhiteSpace(instance.DatabaseType) ||
                string.IsNullOrWhiteSpace(instance.ConnectionString))
            {
                return new BootstrapBindingResult(false, "Instance database settings are invalid.");
            }

            lock (Sync)
            {
                configuration["Database:Type"] = instance.DatabaseType;
                configuration["Database:ConnectionString"] = instance.ConnectionString;

                var optionsType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("IV.DX.Persistence.DXDatabaseOptions", throwOnError: false))
                    .FirstOrDefault(type => type is not null)
                    ?? Type.GetType("IV.DX.Persistence.DXDatabaseOptions, IV.DX.Persistence", throwOnError: false);

                if (optionsType is null)
                {
                    return new BootstrapBindingResult(false, "DXDatabaseOptions type was not found.");
                }

                var optionsCacheType = typeof(IOptionsMonitorCache<>).MakeGenericType(optionsType);
                var optionsCache = rootServiceProvider.GetService(optionsCacheType);
                if (optionsCache is null)
                {
                    return new BootstrapBindingResult(false, "DX options cache service was not found.");
                }

                var clearMethod = optionsCacheType.GetMethod("Clear");
                if (clearMethod is null)
                {
                    return new BootstrapBindingResult(false, "DX options cache clear method was not found.");
                }

                clearMethod.Invoke(optionsCache, null);
            }

            return new BootstrapBindingResult(true);
        }
    }
}
