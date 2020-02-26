using System;
using System.Collections.Generic;
using System.Linq;
using CustomerApi.Models;

namespace CustomerApi.Data
{
    public class DbInitializer : IDbInitializer
    {
        // This method will create and seed the database.
        public void Initialize(CustomerApiContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            if (context.Customers.Any())
            {
                // Check if DB has been seeded.
                return;
            }

            List<Customer> customers = new List<Customer>
            {
                new Customer { Id = 1, Name = "customer1", Email = "e1@mail.com", PhoneNumber = "1234", BillingAddress = "billingAddress1", ShippingAddress = "shippingAddress1", CreditStanding = true}
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();
        }
    }
}
