using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Integration.Common;
using Integration.Backend;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    
    // This is a cache of the items that have been saved where the key is the
    // hash code of the item content. This is used to prevent duplicate items.
    // The reason for using a hash code is that it is much faster to compare
    // hash codes than it is to compare strings. The memory performance is as follows:
    // 1. The hash code is 8 bytes.
    // 2. The string is 2 bytes per character.
    // 3. The string is UTF-16 encoded, so it is 2 bytes per character.
    // 4. The string is 40 characters long, so it is 80 bytes.
    // 5. The hash code is 8 bytes.
    // 6. The hash code is 10 times smaller than the string.
    // 7. The hash code is 10 times faster to compare than the string.
    
    // Finally, let's say we have 1 gigabyte of memory just for this cache.
    // We could have 10^9 / 8 = 125,000,000 items in the cache.
    // If this is not enough, we can also implement a expiration mechanism.
    private HashSet<long> SavedItems { get; set; } = new();
    // Simple lock mechanism
    // We could use concurrent collection of some sort, but that requires
    // additional work. That is, when you check if the item is saved, you
    // need to lock the collection. It is because when you are checking
    // the collection, another thread might be adding an item to the collection.
    private readonly object _lock = new();

    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent)
    {
        // Check the backend to see if the content is already saved.
        if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }

        // Check the cache to see if the content is already saved.
        var isSaved = CheckAndSaveContent(itemContent);
        if (isSaved)
        {
            return new Result(false, $"Duplicate item received with content {itemContent}.");
        }
        var item = ItemIntegrationBackend.SaveItem(itemContent);

        return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
    }

    private bool CheckAndSaveContent(string itemContent)
    {
        // Get the hash code of the item content.
        var itemContentHashCode = GetInt64HashCode(itemContent);
        var result = false;
        // Check the cache to see if the content is already saved.
        lock (_lock)
        {
            if (SavedItems.Contains(itemContentHashCode))
            {
                Console.WriteLine($"The item is saved before: {itemContent}");
                result = true;
            }
            else
            {
                // Add the hash code to the cache before saving the actual item due to the response time
                // in the backend. This is to prevent duplicate items from being saved.
                Console.WriteLine($"The item is now being saved: {itemContent}");
                SavedItems.Add(itemContentHashCode);
            } 
        }
        return result;
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
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