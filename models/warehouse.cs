using System.Collections.Generic;

public class Warehouse
{
    public string Id { get; set; }
    public Dictionary<string, int> Inventory { get; set; }

    public Warehouse(string id, Dictionary<string, int> inventory)
    {
        Id = id;
        Inventory = inventory;
    }

    public bool HasInventory(string product, int quantity)
    {
        return Inventory.ContainsKey(product) && Inventory[product] >= quantity;
    }

    public void SubtractFromInventory(string product, int quantity)
    {
        if (HasInventory(product, quantity))
        {
            Inventory[product] -= quantity;
        }
    }
}