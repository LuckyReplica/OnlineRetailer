using System;
using System.Collections.Generic;

namespace OrderApi.Models
{
    public class Order
    {
        public enum Status
        {
            Completed = 1,
            Cancelled = 2,
            Shipped = 3,
            Paid = 4
        };

        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public List<ProductDTO> Products { get; set; }
        public int CustomerID { get; set; }
        public Status StatusCode { get; set; }
    }
}
