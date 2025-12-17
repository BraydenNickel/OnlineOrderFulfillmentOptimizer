using System.Collections.Generic;

namespace OnlineOrderFulfillmentOptimizer.Models
{
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
            if (!Inventory.ContainsKey(product.ProductId))
                Inventory[product.ProductId] = 0;

            Inventory[product.ProductId] += product.Quantity;
        }

        public bool HasInventory(string productId, int quantity)
        {
            return Inventory.ContainsKey(productId) && Inventory[productId] >= quantity;
        }

        public void SubtractFromInventory(string product, int quantity)
        {
            if (HasInventory(product, quantity))
            {
                Inventory[product] -= quantity;
            }
        }
    }
}