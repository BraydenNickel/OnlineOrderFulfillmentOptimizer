using System;

namespace OnlineOrderFulfillmentOptimizer.Exceptions
{
    public class InvalidOrderException : Exception
    {
        public int OrderId { get; private set; }

        public InvalidOrderException(int orderId, string message) 
        : base($"Order {orderId}: {message}")
        {
            OrderId = orderId;
        }
    }
}
    