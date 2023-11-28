using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;


namespace Integration.Common;

public class DistributedCache : IDistributedCache
{
    // This class is intended to utilize Redis or similar distributed cache.
    // For the sake of the task, this will behave as in-memory.
    
    private readonly MemoryCache _cache = MemoryCache.Default;
    public Result SaveItem(Item item)
    {
        var key = GetInt64HashCode(item.Content);
        var cacheItem = _cache.GetCacheItem(key.ToString());
        if (cacheItem is {Value: Item tempItem})
        {
            return new Result(false, $"Duplicate item received with content {tempItem.Content}.");
        }
        // This simulates how long it takes to save
        Thread.Sleep(100);
        
        // There is no need to save content as well. It just is for the convenience.
        _cache.Set(new CacheItem(key.ToString(), item), new CacheItemPolicy());
        return new Result(true, "Item saved.");
    }

    public Result SaveItem(Item item, DateTimeOffset offset)
    {
        var key = GetInt64HashCode(item.Content);
        var cacheItem = _cache.GetCacheItem(key.ToString());
        if (cacheItem is {Value: Item tempItem})
        {
            return new Result(false, $"Duplicate item received with content {tempItem.Content}.");
        }
        // This simulates how long it takes to save
        Thread.Sleep(100);
        
        // There is no need to save content as well. It just is for the convenience.
        _cache.Set(new CacheItem(key.ToString(), item), new CacheItemPolicy()
        {
            AbsoluteExpiration = offset
        });
        return new Result(true, "Item saved.");
    }

    public Item? GetItem(long key)
    {
        var cacheItem = _cache.GetCacheItem(key.ToString());
        return cacheItem is {Value: Item tempItem} ? tempItem : null;
    }

    public List<Item> GetAllItems()
    {
        var items = new List<Item>();
        foreach (var item in _cache)
        {
            items.Add(item.Value as Item);
        }
        return items;
    }

    private static long GetInt64HashCode(string strText)
    { 
        long hashCode = 0;
        if (string.IsNullOrEmpty(strText)) return (hashCode);
        //Unicode Encode Covering all character set
        var byteContents = Encoding.Unicode.GetBytes(strText);
        var hashText = SHA256.HashData(byteContents);
        hashCode = BitConverter.ToInt64(hashText);
        return (hashCode);
    }
}