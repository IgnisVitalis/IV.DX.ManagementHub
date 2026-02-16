namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapRuntimeState
    {
        private int _isHandlersInitialized;
        private readonly HashSet<string> _activatedInstances = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _activatedDatabases = new(StringComparer.Ordinal);
        private readonly object _sync = new();
        private string? _currentInstanceKey;

        public BootstrapRuntimeState(bool isHandlersInitialized = false)
        {
            _isHandlersInitialized = isHandlersInitialized ? 1 : 0;
        }

        public bool IsDxRuntimeEnabled
        {
            get
            {
                lock (_sync)
                {
                    return _activatedInstances.Count > 0;
                }
            }
        }

        public string? CurrentInstanceKey
        {
            get
            {
                lock (_sync)
                {
                    return _currentInstanceKey;
                }
            }
        }

        public bool IsHandlersInitialized => Volatile.Read(ref _isHandlersInitialized) == 1;

        public void MarkHandlersInitialized()
        {
            Interlocked.Exchange(ref _isHandlersInitialized, 1);
        }

        public bool IsInstanceActivated(string instanceKey)
        {
            if (string.IsNullOrWhiteSpace(instanceKey))
            {
                return false;
            }

            lock (_sync)
            {
                return _activatedInstances.Contains(instanceKey.Trim());
            }
        }

        public void MarkInstanceActivated(string instanceKey)
        {
            if (string.IsNullOrWhiteSpace(instanceKey))
            {
                return;
            }

            lock (_sync)
            {
                _activatedInstances.Add(instanceKey.Trim());
                _currentInstanceKey = instanceKey.Trim();
            }
        }

        public bool IsDatabaseActivated(string databaseType, string connectionString)
        {
            var databaseKey = BuildDatabaseKey(databaseType, connectionString);
            if (string.IsNullOrWhiteSpace(databaseKey))
            {
                return false;
            }

            lock (_sync)
            {
                return _activatedDatabases.Contains(databaseKey);
            }
        }

        public void MarkDatabaseActivated(string databaseType, string connectionString)
        {
            var databaseKey = BuildDatabaseKey(databaseType, connectionString);
            if (string.IsNullOrWhiteSpace(databaseKey))
            {
                return;
            }

            lock (_sync)
            {
                _activatedDatabases.Add(databaseKey);
            }
        }

        public void MarkCurrentInstance(string instanceKey)
        {
            if (string.IsNullOrWhiteSpace(instanceKey))
            {
                return;
            }

            lock (_sync)
            {
                _currentInstanceKey = instanceKey.Trim();
            }
        }

        private static string BuildDatabaseKey(string databaseType, string connectionString)
        {
            return BootstrapDatabaseIdentity.BuildKey(databaseType, connectionString);
        }
    }
}
