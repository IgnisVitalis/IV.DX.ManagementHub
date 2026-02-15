namespace IV.ManagementHub.ApiService.Bootstrap
{
    public sealed class BootstrapRuntimeState
    {
        private int _isDxRuntimeEnabled;
        private int _isHandlersInitialized;

        public BootstrapRuntimeState(bool isDxRuntimeEnabled = false, bool isHandlersInitialized = false)
        {
            _isDxRuntimeEnabled = isDxRuntimeEnabled ? 1 : 0;
            _isHandlersInitialized = isHandlersInitialized ? 1 : 0;
        }

        public bool IsDxRuntimeEnabled => Volatile.Read(ref _isDxRuntimeEnabled) == 1;

        public bool IsHandlersInitialized => Volatile.Read(ref _isHandlersInitialized) == 1;

        public void MarkHandlersInitialized()
        {
            Interlocked.Exchange(ref _isHandlersInitialized, 1);
        }

        public void MarkRuntimeEnabled()
        {
            Interlocked.Exchange(ref _isDxRuntimeEnabled, 1);
        }
    }
}
