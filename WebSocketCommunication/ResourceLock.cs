
namespace WebSocketCommunication
{
    internal class ResourceLock<TSource> where TSource : class, new()
    {
        #region Fields
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private TSource _resource;
        #endregion

        #region Methods
        public ResourceLock()
        {
            _resource = new TSource();
        }

        public async void LockAsync(Action<TSource> lambda)
        {
            await _semaphore.WaitAsync();
            try { lambda.Invoke(_resource); }
            finally { _semaphore.Release(); }
        }

        public void Release() => _semaphore.Release();
        #endregion
    }
}
