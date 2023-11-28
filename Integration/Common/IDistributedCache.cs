namespace Integration.Common;

public interface IDistributedCache
{
    Result SaveItem(Item item);
    Result SaveItem(Item item, DateTimeOffset offset);
    Item? GetItem(long key);
    List<Item> GetAllItems();
}