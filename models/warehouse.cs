using System.Collections.Generic;

public class Warehouse
{
    public string Id { get; set; }
    public Dictionary<string, int> Inventory { get; set; }

    public Warehouse(string id)
    {
        Id = id;
        Inventory = new Dictionary<string, int>();
    }

    public void AddProduct(Product product)
    {
        if (!Inventory.ContainsKey(product.productId))
        {
            Inventory[product.productId] = 0;
        }
        Inventory[product.productId]+=product.quantity;
    }

    public bool HasInventory(string product, int quantity)
    {
        return Inventory.ContainsKey(product.productId) && Inventory[product.productId] >= quantity;
    }

    public void SubtractFromInventory(string product, int quantity)
    {
        if (HasInventory(product, quantity))
        {
            Inventory[product] -= quantity;
        }
    }
}