using Fulfillment.Domain;
using Fulfillment.Exceptions;
using Fulfillment.Validation;

namespace Fulfillment.Logic;

public class FulfillmentEngine
{
    private readonly List<Warehouse> _warehouses;
    private readonly OrderValidator _orderValidator;
    private readonly InventoryValidator _inventoryValidator;

    public FulfillmentEngine(List<Warehouse> warehouses)
    {
        _warehouses = warehouses ?? new List<Warehouse>();
        _orderValidator = new OrderValidator();
        _inventoryValidator = new InventoryValidator(_warehouses);
    }

    public FulfillmentResult ProcessOrders(List<Order> orders)
    {
        var plans = new List<FulfillmentPlan>();
        var failures = new List<OrderFailure>();

        // ✅ Sequential validation now (easy to parallelize later)
        var validOrders = new List<Order>();
        foreach (var order in orders)
        {
            try
            {
                _orderValidator.Validate(order);
                _inventoryValidator.ValidateStockPossible(order);
                validOrders.Add(order);
            }
            catch (Exception ex)
            {
                failures.Add(new OrderFailure(order?.OrderId ?? "(no id)", ex.Message));
            }
        }

        // ✅ Allocation (inventory mutation)
        foreach (var order in validOrders.OrderBy(o => o.OrderId))
        {
            try
            {
                plans.Add(Allocate(order));
            }
            catch (Exception ex)
            {
                failures.Add(new OrderFailure(order.OrderId, ex.Message));
            }
        }

        return new FulfillmentResult(plans, failures);
    }

    private FulfillmentPlan Allocate(Order order)
    {
        // 1) Try single warehouse first (min shipments)
        var candidates = _warehouses.Where(w => w.CanFulfill(order.Items)).ToList();
        if (candidates.Count > 0)
        {
            var chosen = candidates
                .OrderBy(w => ScoreLeftover(w, order.Items)) // simple “efficiency” heuristic
                .First();

            chosen.Reserve(order.Items);

            return new FulfillmentPlan(order.OrderId, new List<ShipmentAllocation>
            {
                new ShipmentAllocation(chosen.Id, new Dictionary<string,int>(order.Items))
            });
        }

        // 2) Otherwise split across warehouses (greedy)
        return AllocateSplit(order);
    }

    private FulfillmentPlan AllocateSplit(Order order)
    {
        // remaining requirements
        var remaining = new Dictionary<string, int>(order.Items);
        var rawShipments = new List<ShipmentAllocation>();

        foreach (var (product, qtyNeededOriginal) in remaining.ToList())
        {
            int need = qtyNeededOriginal;

            foreach (var w in _warehouses.OrderByDescending(x => x.GetAvailable(product)))
            {
                int have = w.GetAvailable(product);
                if (have <= 0) continue;

                int take = Math.Min(have, need);
                if (take <= 0) continue;

                var alloc = new Dictionary<string, int> { [product] = take };
                w.Reserve(alloc);

                rawShipments.Add(new ShipmentAllocation(w.Id, alloc));
                need -= take;

                if (need == 0) break;
            }

            if (need > 0)
                throw new OutOfStockException($"Could not fully allocate '{product}' for order {order.OrderId}.");
        }

        // merge allocations per warehouse for cleaner output
        var merged = rawShipments
            .GroupBy(s => s.WarehouseId)
            .Select(g => new ShipmentAllocation(
                g.Key,
                g.SelectMany(x => x.Items)
                 .GroupBy(kv => kv.Key)
                 .ToDictionary(x => x.Key, x => x.Sum(v => v.Value))
            ))
            .ToList();

        return new FulfillmentPlan(order.OrderId, merged);
    }

    private static int ScoreLeftover(Warehouse w, Dictionary<string, int> items)
    {
        // smaller leftover = better (reduces fragmentation)
        return items.Sum(kv => w.GetAvailable(kv.Key) - kv.Value);
    }
}
