using System;
using System.Threading;
using CustomerApi.Data;
using SharedModels;
using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerApi.Infrastructure
{
    public class MessageListener
    {
        IServiceProvider provider;
        string connectionString;

        // The service provider is passed as a parameter, because the class needs
        // access to the product repository. With the service provider, we can create
        // a service scope that can provide an instance of the product repository.
        public MessageListener(IServiceProvider provider, string connectionString)
        {
            this.provider = provider;
            this.connectionString = connectionString;
        }

        public void Start()
        {
            using (var bus = RabbitHutch.CreateBus(connectionString))
            {
                //bus.Respond<CustomerRequest, ReturnedCustomer>(request => new ReturnedCustomer { customer = HandleCustomerRequest(request.Id) });
                bus.Respond<CustomerRequest, Customer>(request => HandleCustomerRequest(request.Id));
                //bus.Respond<CustomerRequest, Customer>(request => new Customer { Id = 1, Name = "customer1", Email = "e1@mail.com", PhoneNumber = "1234", BillingAddress = "billingAddress1", ShippingAddress = "shippingAddress1", CreditStanding = true });

                // block the thread so that it will not exit and stop subscribing.
                lock (this)
                {
                    Monitor.Wait(this);
                }
            }
        }

        private Customer HandleCustomerRequest(int id)
        {
            // A service scope is created to get an instance of the Customer repository.
            // When the service scope is disposed, the customer repository instance will
            // also be disposed.
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var customerRepos = services.GetService<IRepository<Models.Customer>>();
                var localCustomer = customerRepos.Get(id);
                var customer = new SharedModels.Customer()
                {
                    Id = localCustomer.Id,
                    BillingAddress = localCustomer.BillingAddress,
                    CreditStanding = localCustomer.CreditStanding
                ,
                    Email = localCustomer.Email,
                    Name = localCustomer.Name,
                    PhoneNumber = localCustomer.PhoneNumber,
                    ShippingAddress = localCustomer.ShippingAddress
                };
                return customer;
            }
        }
    }
}
