using System;

namespace OnlineOrderFulfillmentOptimizer.Exceptions
{
    public class WarehouseDoesNotExistException : Exception
    {
        public int WarehouseId { get; private set; }

        public WarehouseDoesNotExistException(int warehouseId, string message) 
        : base($"Warehouse does not exist {warehouseId}: {message}")
        {
            WarehouseId = warehouseId;
        }
    }
}