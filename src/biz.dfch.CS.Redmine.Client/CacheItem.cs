using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace biz.dfch.CS.Redmine.Client
{
    public enum CacheItemType
    {
        Entity,
        Collection
    }

    public struct CacheItem<T>
        where T: IEquatable<T>
    {
        public CacheItem(string key, T item, CacheItemType cacheItemType)
        {
            Key = key;
            Item = item;
            CacheItemType = cacheItemType;
        }

        public string Key;
        public T Item;
        public CacheItemType CacheItemType;
    }
}
