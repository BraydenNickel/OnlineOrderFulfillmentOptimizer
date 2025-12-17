using System;

namespace OnlineOrderFulfillmentOptimizer.Exceptions
{
    public class UnknownProductException : Exception
    {
        public int OrderId { get; private set; }
        public string ProductId { get; private set; }

        public string ProductName{ get; private set; }

        public UnknownProductException(int orderId, string productName, string productId) 
        : base($"Order {orderId}: Unknown product {productName}: ID: {productId}")
        {
            OrderId = orderId;
            ProductName = productName;
            ProductId = productId;
        }
    }
}