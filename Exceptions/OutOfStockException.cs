using System;

namespace OnlineOrderFulfillmentOptimizer.Exceptions
{
    public class OutOfStockException : Exception
    {
        public int OrderId { get; private set; }
        public string ProductId { get; private set; }
        public int RequestedQuantity { get; private set; }

        public OutOfStockException(int orderId, string productId, int requestedQuantity)
        : base($"Order {orderId}: Out of stock for {productId} requested quantity {requestedQuantity}")
        {
            OrderId = orderId;
            ProductId = productId;
            RequestedQuantity = requestedQuantity;
        }
    }
}