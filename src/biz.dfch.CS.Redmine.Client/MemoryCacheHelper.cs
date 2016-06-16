using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using biz.dfch.CS.Utilities.Logging;

namespace biz.dfch.CS.Redmine.Client
{
    public enum ItemCachePriority
    {
        Default,
        NotRemovable
    }

    public class MemoryCacheHelper
    {
        private readonly int _offsetInMinutes;
        private readonly static MemoryCache _cache = MemoryCache.Default;

        public MemoryCacheHelper()
            : this(10)
        {

        }

        public MemoryCacheHelper(int offsetInMinutes)
        {
            _offsetInMinutes = offsetInMinutes;
        }

        public void AddOrUpdate<T>(string key, T item)
            where T : class, IEquatable<T>
        {
            AddOrUpdate(key, item, ItemCachePriority.Default, DateTimeOffset.Now.AddMinutes(_offsetInMinutes));
        }

        public void AddOrUpdate<T>(string key, T item, ItemCachePriority priority)
            where T : class, IEquatable<T>
        {
            AddOrUpdate(key, item, priority, DateTimeOffset.Now.AddMinutes(_offsetInMinutes));
        }

        public static void AddOrUpdate<T>(string key, T item, ItemCachePriority priority,
            DateTimeOffset absoluteExpiration)
            where T : class, IEquatable<T>
        {
            var cachePriority = (priority == ItemCachePriority.Default
                ? CacheItemPriority.Default
                : CacheItemPriority.NotRemovable);

            var policy = new CacheItemPolicy
            {
                Priority = cachePriority,
                AbsoluteExpiration = absoluteExpiration,
                RemovedCallback = CacheEntryRemoved,
                //UpdateCallback = CacheEntryUpdated
            };

            if (_cache.Contains(key))
            {
                _cache.Remove(key);
            }

            _cache.Set(key, item, policy);
        }

        public IEnumerable<T> GetAll<T>()
            where T : class, IEquatable<T>
        {
            var items = _cache
                .Where(p => p.Key.StartsWith(typeof(T).Name))
                .Select(p => (T)p.Value);

            return items;
        }

        public T Get<T>(string key)
            where T : class, IEquatable<T>
        {
            return _cache.Get(key) as T;
        }

        public void Remove<T>(string key)
            where T : IEquatable<T>
        {
            if (!_cache.Contains(key))
                return;

            _cache.Remove(key);
        }

        private static void CacheEntryUpdated(CacheEntryUpdateArguments arguments)
        {
            var msg = string.Format("Reason: {0} | Key: {1} | Item: {2}",
                arguments.RemovedReason,
                arguments.Key,
                arguments.UpdatedCacheItem.Value);

            Trace.WriteLine(msg);
        }

        private static void CacheEntryRemoved(CacheEntryRemovedArguments arguments)
        {
            var msg = string.Format("Reason: {0} | Key: {1} | Item: {2}",
                arguments.RemovedReason,
                arguments.CacheItem.Key,
                arguments.CacheItem.Value);

            Trace.WriteLine(msg);
        }
    }
}
