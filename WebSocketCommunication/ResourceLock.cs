using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSocketCommunication
{
    internal class ResourceLock<TSource> where TSource : new()
    {
        #region Fields
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        // TODO: figure out how to prevent a lock hoder from accessing the resource after they have released it.
        private TSource _resource;
        #endregion

        #region Properties
        #endregion

        #region
        public ResourceLock()
        {
            _resource = new TSource();
        }

        public async Task<TSource> LockAsync()
        {
            return _resource;
        }

        public void Release() => _semaphore.Release();
        #endregion
    }
}
