using System;
using System.Collections.Generic;

namespace LoadBalancer.Models
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int CustomerID { get; set; }
    }
}
