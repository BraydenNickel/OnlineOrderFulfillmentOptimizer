using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using OnlineOrderFulfillmentOptimizer.Exceptions;

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

            var validOrders = new ConcurrentBag<Order>();
            var failureBag = new ConcurrentBag<OrderFailure>();
            var unexpectedFailures = new ConcurrentBag<Exception>();

            Parallel.ForEach(orders, order =>
            {
                try
                {
                    TryValidateOrder(order, out string reason);
                    TryValidateStockPossible(order, out string reason2);
                    validOrders.Add(order);
                }
                catch (InvalidOrderException ex) 
                {
                    failureBag.Add(new OrderFailure(order?.OrderId ?? -1, ex.Message));
                }
                catch (NoFulfillmentPathException ex)
                {
                    failureBag.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (OutOfStockException ex)
                {
                    failureBag.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (UnknownProductException ex)
                {
                    failureBag.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (WarehouseDoesNotExistException ex)
                {
                    failureBag.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (Exception ex)
                {
                    unexpectedFailures.Add(ex);
                    failureBag.Add(new OrderFailure(order.OrderId, $"Unexpected error: {ex.Message}"));
                }
            });
            
            if (!unexpectedFailures.IsEmpty)
                throw new ConcurrentValidationException("One or more unexpected errors occurred during order validation.", unexpectedFailures.ToList());

                failures.AddRange(failureBag);
                orders = validOrders.ToList();

            foreach (var order in validOrders)
            {
                try
                {
                    if (!TryAllocate(order, out var plan, out string reason))
                        throw new NoFulfillmentPathException(order.OrderId, reason);

                    plans.Add(plan);
                }
                catch (NoFulfillmentPathException ex)
                {
                    failures.Add(new OrderFailure(ex.OrderId, ex.Message));
                }
                catch (WarehouseDoesNotExistException ex)
                {
                    failures.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (OutOfStockException ex)
                {
                    failures.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (UnknownProductException ex)
                {
                    failures.Add(new OrderFailure(order.OrderId, ex.Message));
                }
                catch (Exception ex)
                {
                    failures.Add(new OrderFailure(order.OrderId, $"Allocation error: {ex.Message}"));
                }
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

            try
            {
                if (order == null)
                    throw new InvalidOrderException(-1, "Order is null.");

                if (order.Items == null || order.Items.Count == 0)
                    throw new InvalidOrderException(order.OrderId, "Order has no items.");

                foreach (var kv in order.Items)
                {
                    if (string.IsNullOrWhiteSpace(kv.Key))
                        throw new InvalidOrderException(order.OrderId, "Order has a blank product key.");

                    if (kv.Value <= 0)
                        throw new InvalidOrderException(order.OrderId, $"Order has invalid qty for '{kv.Key}': {kv.Value}");
                }

                return true;
            }
            catch (InvalidOrderException ex)
            {
                reason = ex.Message;
                return false;
            }
        }

        // SAFE to parallelize (reads inventory only)
        private bool TryValidateStockPossible(Order order, out string reason)
        {
            reason = "";

            foreach (var (productKey, qty) in order.Items)
            {
                try 
                {
                    if (_warehouses.All(w => GetAvailable(w, productKey) <= 0))
                        throw new NoFulfillmentPathException(order.OrderId, $"Product '{productKey}' is not stocked in any warehouse.");
                }
                catch (NoFulfillmentPathException ex)
                {
                    reason = ex.Message;
                    return false;
                }
        
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
                try 
                {
                    if (best == null)
                        throw new NoFulfillmentPathException(order.OrderId, "No single warehouse can fulfill the order.");
                }
                catch (NoFulfillmentPathException ex)
                {
                    reason = ex.Message;
                    return false;
                }
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
            try 
            {
                if (_warehouses == null || _warehouses.Count == 0)
                    throw new NoFulfillmentPathException(-1, "No warehouses available.");
            }
            catch (NoFulfillmentPathException)
            {
                return null;
            }

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
                try 
                {
                    if (_warehouses.All(w => GetAvailableSnapshot(snapshot, w, product) <= 0))
                        throw new NoFulfillmentPathException(order.OrderId, $"Product '{product}' is not stocked in any warehouse.");
                }
                catch (NoFulfillmentPathException ex)
                {
                    reason = ex.Message;
                    return false;
                }

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

    // Output models 
    public record ShipmentAllocation(string WarehouseId, Dictionary<string, int> Items);
    public record FulfillmentPlan(int OrderId, List<ShipmentAllocation> Shipments);

    public record OrderFailure(int OrderId, string Reason);
    public record FulfillmentResult(List<FulfillmentPlan> Plans, List<OrderFailure> Failures);

}