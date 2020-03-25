using EasyNetQ;
using RestSharp;
using SharedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderApi.Infrastructure
{
    public class MessagePublisher : IMessagePublisher, IDisposable
    {
        IBus bus;

        public MessagePublisher(string connectionString)
        {
            bus = RabbitHutch.CreateBus(connectionString);
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        public void PublishOrderStatusChangedMessage(int customerId, IEnumerable<Order.OrderLine> orderLines, string topic)
        {
            var message = new OrderStatusChangedMessage
            {
                CustomerID = customerId,
                OrderLines = orderLines
            };

            bus.Publish(message, topic);
        }

        public void PublishCreditStandingChangedMessage(int customerId, bool creditStanding, string topic)
        {
            var message = new CreditStandingChangeMessage
            {
                ClientId = customerId,
                CreditStanding = creditStanding
            };

            bus.Publish(message, topic);
        }

        public bool IsInStock(Order order)
        {
            RestClient client = new RestClient("http://productapi/api/products/CheckIfInStock");
            var orderLines = new OrderStatusChangedMessage { OrderLines = order.OrderLines };

            var request = new RestRequest(Method.PUT);
            request.AddJsonBody(orderLines.OrderLines);

            var response = client.Execute(request);
            //var response = bus.Request<OrderStatusChangedMessage, IsInSockRequest>(orderLines);
            var boo = bool.Parse(response.Content);

            return boo;
        }

        public Customer RequestCustomer(int id)
        {
            RestClient client = new RestClient("http://customerapi/customers/");

            var request = new RestRequest(id.ToString(), Method.GET);

            var url = client.BuildUri(request);

            var response = client.Execute<Customer>(request);

            return response.Data;

            //var cq = new CustomerRequest { Id = id };
            //var response = bus.Request<CustomerRequest, Customer>(cq);
            //return response;
        }
    }
}
