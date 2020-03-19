using System.Collections.Generic;
using System.Linq;
using SharedModels;
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

            List<Order.OrderLine> products = new List<Order.OrderLine>
            {
                new Order.OrderLine {OrderId = 1, ProductId = 1,  Quantity = 10}
            };

            List<Order> orders = new List<Order>
            {
                new Order { Date = DateTime.Today, OrderLines = products},
                new Order { Date = DateTime.Today}
            };

            context.Orders.AddRange(orders);
            context.SaveChanges();
        }
    }
}
