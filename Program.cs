using System;
using System.Collections.Generic;
using System.Linq;
using OnlineOrderFulfillmentOptimizer.Models;
using OnlineOrderFulfillmentOptimizer.Exceptions;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("=== Online Order Fulfillment Optimizer ===\n");

        // Create ONE product instance per real product (ProductId is generated once)
        var laptop = new Product("Laptop", 1, ProductCategory.Technology);
        var mouse = new Product("Mouse", 1, ProductCategory.Technology);
        var keyboard = new Product("Keyboard", 1, ProductCategory.Technology);

        // ProductId -> Name (for nicer printing)
        var idToName = new Dictionary<string, string>
        {
            [laptop.ProductId] = laptop.Name,
            [mouse.ProductId] = mouse.Name,
            [keyboard.ProductId] = keyboard.Name
        };

        Console.WriteLine("Catalog IDs:");
        foreach (var kv in idToName)
            Console.WriteLine($"  {kv.Value} => {kv.Key}");
        Console.WriteLine();

        // Warehouses (Inventory keys MUST match Order.Items keys -> ProductId)
        var w1 = new Warehouse("W1");
        w1.Inventory[laptop.ProductId] = 5;
        w1.Inventory[mouse.ProductId] = 20;

        var w2 = new Warehouse("W2");
        w2.Inventory[laptop.ProductId] = 2;
        w2.Inventory[keyboard.ProductId] = 10;

        var warehouses = new List<Warehouse> { w1, w2 };

        // Orders (your OrderId is int)
        var orders = new List<Order>
        {
            new Order(1001, new Dictionary<string,int>
            {
                [laptop.ProductId] = 1,
                [mouse.ProductId] = 2
            }),
            new Order(1002, new Dictionary<string,int>
            {
                [keyboard.ProductId] = 1
            }),

            //  failure tests
            new Order(1003, new Dictionary<string,int> { [laptop.ProductId] = 99 }),
            new Order(1004, new Dictionary<string,int> { [mouse.ProductId] = -1 })
        };

        var engine = new FulfillmentEngine(warehouses);
        var result = engine.ProcessOrders(orders);

        PrintPlans(result.Plans, idToName);
        PrintFailures(result.Failures);
        Console.WriteLine("\n=== Ending Inventory ===");
        PrintWarehouseInventory(warehouses, idToName);
    }

    private static void PrintPlans(List<FulfillmentPlan> plans, Dictionary<string, string> idToName)
    {
        Console.WriteLine("=== Fulfillment Plans ===");

        if (plans.Count == 0)
        {
            Console.WriteLine("(none)\n");
            return;
        }

        foreach (var plan in plans)
        {
            Console.WriteLine($"Order {plan.OrderId}:");
            foreach (var ship in plan.Shipments)
                Console.WriteLine($"  From {ship.WarehouseId}: {FormatItems(ship.Items, idToName)}");
            Console.WriteLine();
        }
    }

    private static void PrintFailures(List<OrderFailure> failures)
    {
        Console.WriteLine("=== Failures ===");

        if (failures.Count == 0)
        {
            Console.WriteLine("(none)\n");
            return;
        }

        foreach (var f in failures)
            Console.WriteLine($"Order {f.OrderId}: {f.Reason}");

        Console.WriteLine();
    }

    private static void PrintWarehouseInventory(List<Warehouse> warehouses, Dictionary<string, string> idToName)
    {
        foreach (var w in warehouses)
        {
            Console.WriteLine($"{w.Id}:");

            if (w.Inventory == null || w.Inventory.Count == 0)
            {
                Console.WriteLine("  (empty)");
                continue;
            }

            foreach (var kv in w.Inventory.OrderBy(k => k.Key))
            {
                var name = idToName.TryGetValue(kv.Key, out var n) ? n : kv.Key;
                Console.WriteLine($"  {name} ({kv.Key}): {kv.Value}");
            }
        }
    }

    private static string FormatItems(Dictionary<string, int> items, Dictionary<string, string> idToName)
    {
        return string.Join(", ", items.Select(kv =>
        {
            var name = idToName.TryGetValue(kv.Key, out var n) ? n : kv.Key;
            return $"{name} x{kv.Value}";
        }));
    }
}
