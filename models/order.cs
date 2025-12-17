using System;
using System.Collections.Generic;

public class Order
{
    public int OrderId { get; set; }
    public Dictionary<string, int> Items { get; set; }

    public Order(int orderId, Dictionary<string, int> items)
    {
        OrderId = orderId;
        Items = items;
    }

}