using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        public void AddOrUpdate<T>(string key, CacheItem<T> item)
            where T: IEquatable<T>
        {
            AddOrUpdate(key, item, ItemCachePriority.Default, DateTimeOffset.Now.AddMinutes(_offsetInMinutes));
        }

        public void AddOrUpdate<T>(string key, CacheItem<T> item, ItemCachePriority priority)
                where T: IEquatable<T>
        {
            AddOrUpdate(key, item, priority, DateTimeOffset.Now.AddMinutes(_offsetInMinutes));
        }

        public static void AddOrUpdate<T>(string key, CacheItem<T> item, ItemCachePriority priority,
            DateTimeOffset absoluteExpiration)
                where T: IEquatable<T>
        {
            var cachePriority = (priority == ItemCachePriority.Default
                ? CacheItemPriority.Default
                : CacheItemPriority.NotRemovable);

            var policy = new CacheItemPolicy
            {
                Priority = cachePriority,
                AbsoluteExpiration = absoluteExpiration,
                RemovedCallback = CacheEntryRemoved,
            };

            if (_cache.Contains(key))
            {
                _cache.Remove(key);
            }

            _cache.Set(key, item, policy);
        }

        public IEnumerable<CacheItem<T>> GetAll<T>()
            where T : IEquatable<T>
        {
            return _cache.Select(p=>p.Value).OfType<CacheItem<T>>();
        }

        public IEnumerable<CacheItem<T>> GetAllByClause<T>(Func<CacheItem<T>, bool> whereClause)
            where T : class, IEquatable<T>
        {
            var items = GetAll<T>();

            return whereClause != null
                ? items.Where(whereClause)
                : items;
        }

        public CacheItem<T> Get<T>(string key)
            where T : IEquatable<T>
        {
            return (CacheItem<T>) _cache.Get(key);
        }

        public void Remove(string key)
        {
            if (!_cache.Contains(key))
                return;

            _cache.Remove(key);
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
