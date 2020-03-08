using System;
using System.Collections.Generic;

namespace SharedModels
{
    public class OrderStatusChangedMessage
    {
        public int? CustomerID { get; set; }

        public IEnumerable<Order.OrderLine> OrderLines { get; set; }
    }
}
