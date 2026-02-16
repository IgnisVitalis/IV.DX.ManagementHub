namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapSettingsSnapshot
    {
        private BootstrapSettings? _current;

        public BootstrapSettingsSnapshot(BootstrapSettings? initial)
        {
            _current = initial?.Normalize();
        }

        public BootstrapSettings? Current => Volatile.Read(ref _current);

        public void Set(BootstrapSettings? settings)
        {
            Interlocked.Exchange(ref _current, settings?.Normalize());
        }
    }
}
