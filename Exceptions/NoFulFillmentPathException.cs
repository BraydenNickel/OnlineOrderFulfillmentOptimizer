using System;

namespace OnlineOrderFulfillmentOptimizer.Exceptions
{
    public class NoFulfillmentPathException : Exception
    {
        public int OrderId { get; private set; }

        public NoFulfillmentPathException(int orderId, string message) 
        : base($"Order {orderId}: No fulfillment path for {message}")
        {
            OrderId = orderId;
        }
    }
}