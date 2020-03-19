using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedModels;

namespace OrderApi.Infrastructure
{
    public interface IMessagePublisher
    {
        void PublishOrderStatusChangedMessage(int customerId, IEnumerable<Order.OrderLine> orderLines, string topic);

        void PublishCreditStandingChangedMessage(int customerId, bool creditStanding, string topic);

        Customer RequestCustomer(int id);
    }
}
