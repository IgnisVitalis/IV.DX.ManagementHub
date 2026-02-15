using System.Text.Json;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class JsonBootstrapSettingsStore : IBootstrapSettingsStore
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _sync = new(1, 1);
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public JsonBootstrapSettingsStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Bootstrap settings file path is required.", nameof(filePath));
            }

            _filePath = filePath;
        }

        public async Task<BootstrapSettings?> LoadAsync(CancellationToken ct = default)
        {
            await _sync.WaitAsync(ct);
            try
            {
                if (!File.Exists(_filePath))
                {
                    return null;
                }

                await using var stream = File.OpenRead(_filePath);
                return await JsonSerializer.DeserializeAsync<BootstrapSettings>(stream, _serializerOptions, ct);
            }
            finally
            {
                _sync.Release();
            }
        }

        public async Task SaveAsync(BootstrapSettings settings, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            await _sync.WaitAsync(ct);
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using var stream = File.Create(_filePath);
                await JsonSerializer.SerializeAsync(stream, settings, _serializerOptions, ct);
            }
            finally
            {
                _sync.Release();
            }
        }
    }
}
