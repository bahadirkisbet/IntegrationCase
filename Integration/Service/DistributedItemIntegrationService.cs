using System.Security.Cryptography;
using System.Text;
using Integration.Backend;
using Integration.Common;

namespace Integration.Service;

public class DistributedItemIntegrationService
{
    // this scenario does not work fully with the example in the Program.cs file.
    // it is because the cache does not have all features Redis have.
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    
    // It can be injected via constructor.
    private IDistributedCache DistributedCache { get; } = new DistributedCache();


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
        var item = new Item
        {
            Content = itemContent
        };
        var result = DistributedCache.SaveItem(item);
        return result.Success;
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
    
}