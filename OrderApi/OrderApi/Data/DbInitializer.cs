using System.Collections.Generic;
using System.Linq;
using OrderApi.Models;
using System;

namespace OrderApi.Data
{
    public class DbInitializer : IDbInitializer
    {
        // This method will create and seed the database.
        public void Initialize(OrderApiContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Look for any Products
            if (context.Orders.Any())
            {
                return;   // DB has been seeded
            }

            List<ProductDTO> products = new List<ProductDTO>
            {
                new ProductDTO {OrderId = 1, ProductId = 1, PriceForAll = 10, Quantity = 10}
            };

            List<Order> orders = new List<Order>
            {
                new Order { Date = DateTime.Today, Products = products},
                new Order { Date = DateTime.Today}
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();
        }
    }
}
