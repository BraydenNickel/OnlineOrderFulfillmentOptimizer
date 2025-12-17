using System;
using System.Collections.Generic;
using System.Linq;

namespace OnlineOrderFulfillmentOptimizer.Models
{
    public class FulfillmentEngine
    {
        private readonly List<Warehouse> _warehouses;

        public FulfillmentEngine(List<Warehouse> warehouses)
        {
            _warehouses = warehouses ?? new List<Warehouse>();
        }

        public FulfillmentResult ProcessOrders(List<Order> orders)
        {
            var plans = new List<FulfillmentPlan>();
            var failures = new List<OrderFailure>();

            if (orders == null || orders.Count == 0)
                return new FulfillmentResult(plans, failures);

        
            //  PARALLELIZATION WOULD GO HERE
        
            //   - TryValidateOrder(order, out reason)
            //   - TryValidateStockPossible(order, out reason)  (reads inventory totals)
            //
            // Example idea:
            //   Parallel.ForEach(orders, order => { validate -> put into thread-safe validOrders/failures })
            //
            // Then AFTER that, do allocation sequentially.
            //
            // DO NOT parallelize allocation unless you add locks / careful synchronization.
    

            foreach (var order in orders)
            {
                // 1) validate order structure
                //  SAFE to parallelize (read-only, no inventory changes)
                if (!TryValidateOrder(order, out string reason))
                {
                    failures.Add(new OrderFailure(order?.OrderId ?? -1, reason));
                    continue;
                }

                // 2) validate inventory possible across ALL warehouses (no reservation yet)
                //  SAFE to parallelize (read-only inventory checks)
                if (!TryValidateStockPossible(order, out reason))
                {
                    failures.Add(new OrderFailure(order.OrderId, reason));
                    continue;
                }

                // 3) allocate efficiently (mutates inventory)
                //  NOT safe to parallelize without locks because it subtracts inventory.
                if (!TryAllocate(order, out var plan, out reason))
                {
                    failures.Add(new OrderFailure(order.OrderId, reason));
                    continue;
                }

                plans.Add(plan);
            }

            return new FulfillmentResult(plans, failures);
        }

        // -------------------------
        // Validation (no exceptions)
        // -------------------------
        // SAFE to parallelize
        private bool TryValidateOrder(Order order, out string reason)
        {
            reason = "";

            if (order == null)
            {
                reason = "Order is null.";
                return false;
            }

            if (order.Items == null || order.Items.Count == 0)
            {
                reason = $"Order {order.OrderId} has no items.";
                return false;
            }

            foreach (var kv in order.Items)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    reason = $"Order {order.OrderId} has a blank product key.";
                    return false;
                }

                if (kv.Value <= 0)
                {
                    reason = $"Order {order.OrderId} has invalid qty for '{kv.Key}': {kv.Value}";
                    return false;
                }
            }

            return true;
        }

        // SAFE to parallelize (reads inventory only)
        private bool TryValidateStockPossible(Order order, out string reason)
        {
            reason = "";

            foreach (var (productKey, qty) in order.Items)
            {
        
                int total = _warehouses.Sum(w => GetAvailable(w, productKey));
                if (total < qty)
                {
                    reason = $"Out of stock: '{productKey}'. Need {qty}, have {total} total.";
                    return false;
                }
            }

            return true;
        }

        // -------------------------
        // Allocation (efficient)
        // -------------------------
    
        private bool TryAllocate(Order order, out FulfillmentPlan plan, out string reason)
        {
            plan = null!;
            reason = "";

            // A) Try single warehouse first (min shipments)
            var best = FindBestSingleWarehouse(order.Items);
            if (best != null)
            {
                ApplyReservation(best, order.Items);

                plan = new FulfillmentPlan(order.OrderId, new List<ShipmentAllocation>
                {
                    new ShipmentAllocation(best.Id, new Dictionary<string,int>(order.Items))
                });
                return true;
            }

            // B) Otherwise split across warehouses (greedy), using a snapshot first (no partial subtract)
            // snapshot planning is safe, BUT applying reservations at the end mutates inventory.
            return TryAllocateSplit(order, out plan, out reason);
        }

    
        private Warehouse? FindBestSingleWarehouse(Dictionary<string, int> items)
        {
            var candidates = _warehouses
                .Where(w => items.All(kv => GetAvailable(w, kv.Key) >= kv.Value))
                .ToList();

            if (candidates.Count == 0) return null;

            return candidates.OrderBy(w => ScoreLeftover(w, items)).First();
        }

    
        private bool TryAllocateSplit(Order order, out FulfillmentPlan plan, out string reason)
        {
            plan = null!;
            reason = "";

            
            var snapshot = _warehouses.ToDictionary(
                w => w,
                w => new Dictionary<string, int>(w.Inventory ?? new Dictionary<string, int>())
            );

            var alloc = new Dictionary<Warehouse, Dictionary<string, int>>();

            foreach (var (product, qtyNeeded) in order.Items)
            {
                int need = qtyNeeded;

                foreach (var w in _warehouses.OrderByDescending(x => GetAvailableSnapshot(snapshot, x, product)))
                {
                    int have = GetAvailableSnapshot(snapshot, w, product);
                    if (have <= 0) continue;

                    int take = Math.Min(have, need);
                    if (take <= 0) continue;

                    snapshot[w][product] = have - take;

                    if (!alloc.ContainsKey(w))
                        alloc[w] = new Dictionary<string, int>();

                    if (!alloc[w].ContainsKey(product))
                        alloc[w][product] = 0;

                    alloc[w][product] += take;

                    need -= take;
                    if (need == 0) break;
                }

                if (need > 0)
                {
                    reason = $"Could not fully allocate '{product}' for order {order.OrderId}.";
                    return false;
                }
            }

        
            foreach (var entry in alloc)
                ApplyReservation(entry.Key, entry.Value);

            var shipments = alloc
                .Select(kvp => new ShipmentAllocation(kvp.Key.Id, kvp.Value))
                .ToList();

            plan = new FulfillmentPlan(order.OrderId, shipments);
            return true;
        }

        // -------------------------
        // Helpers
        // -------------------------
        private static int GetAvailable(Warehouse w, string productKey)
        {
            if (w.Inventory == null) return 0;
            return w.Inventory.TryGetValue(productKey, out int qty) ? qty : 0;
        }

        private static int GetAvailableSnapshot(
            Dictionary<Warehouse, Dictionary<string, int>> snapshot,
            Warehouse w,
            string productKey)
        {
            return snapshot[w].TryGetValue(productKey, out int qty) ? qty : 0;
        }

        private static int ScoreLeftover(Warehouse w, Dictionary<string, int> items)
            => items.Sum(kv => GetAvailable(w, kv.Key) - kv.Value);

        private static void ApplyReservation(Warehouse w, Dictionary<string, int> items)
        {
            if (w.Inventory == null)
                w.Inventory = new Dictionary<string, int>();

            foreach (var (productKey, qty) in items)
            {
                if (!w.Inventory.ContainsKey(productKey))
                    w.Inventory[productKey] = 0;

                w.Inventory[productKey] -= qty;
            }
        }
    }

    // Output models (no exceptions)
    public record ShipmentAllocation(string WarehouseId, Dictionary<string, int> Items);
    public record FulfillmentPlan(int OrderId, List<ShipmentAllocation> Shipments);

    public record OrderFailure(int OrderId, string Reason);
    public record FulfillmentResult(List<FulfillmentPlan> Plans, List<OrderFailure> Failures);

}