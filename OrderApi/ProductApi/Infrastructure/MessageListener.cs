using EasyNetQ;
using Microsoft.Extensions.DependencyInjection;
using ProductApi.Data;
using SharedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductApi.Infrastructure
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
                bus.Subscribe<OrderStatusChangedMessage>("productApiHkCompleted", HandleOrderCompleted, x => x.WithTopic("completed"));

                bus.Subscribe<OrderStatusChangedMessage>("productApiCancelledCompleted", HandleOrderCancelled, x => x.WithTopic("cancelled"));

                bus.Subscribe<OrderStatusChangedMessage>("productApiShipped", HandleOrderShipped, x => x.WithTopic("shipped"));

                bus.Respond<OrderStatusChangedMessage, IsInSockRequest>(request => HandleCheckIfInStock(request));

                // Block the thread so that it will not exit and stop subscribing.
                lock (this)
                {
                    Monitor.Wait(this);
                }
            }

        }

        private void HandleOrderCompleted(OrderStatusChangedMessage message)
        {
            // A service scope is created to get an instance of the product repository.
            // When the service scope is disposed, the product repository instance will
            // also be disposed.
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                foreach (var orderLine in message.OrderLines)
                {
                    var product = productRepos.Get(orderLine.ProductId);
                    product.ItemsReserved += orderLine.Quantity;
                    product.ItemsInStock -= orderLine.Quantity;
                    productRepos.Edit(product);
                }
            }
        }

        private void HandleOrderCancelled(OrderStatusChangedMessage message)
        {
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                foreach (var orderLine in message.OrderLines)
                {
                    var product = productRepos.Get(orderLine.ProductId);
                    product.ItemsInStock += orderLine.Quantity;
                    product.ItemsReserved -= orderLine.Quantity;
                    productRepos.Edit(product);
                }
            }
        }

        private void HandleOrderShipped(OrderStatusChangedMessage message)
        {
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                foreach (var orderLine in message.OrderLines)
                {
                    var product = productRepos.Get(orderLine.ProductId);
                    product.ItemsReserved -= orderLine.Quantity;
                    productRepos.Edit(product);
                }
            }
        }

        private IsInSockRequest HandleCheckIfInStock(OrderStatusChangedMessage message)
        {
            using (var scope = provider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var productRepos = services.GetService<IRepository<Product>>();

                foreach (var orderLine in message.OrderLines)
                {
                    var product = productRepos.Get(orderLine.ProductId);

                    if (product.ItemsInStock < orderLine.Quantity)
                    {
                        return new IsInSockRequest { isInStock = false };
                    }                
                }
            }
            return new IsInSockRequest { isInStock = true };
        }
    }
}
