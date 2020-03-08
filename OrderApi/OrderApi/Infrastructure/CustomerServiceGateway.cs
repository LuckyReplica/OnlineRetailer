using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RestSharp;
using SharedModels;

namespace OrderApi.Infrastructure
{
    public class CustomerServiceGateway : IServiceGateway<Customer>
    {
        Uri customerServiceBaseUrl;

        public CustomerServiceGateway(Uri baseUrl)
        {
            customerServiceBaseUrl = baseUrl;
        }

        public Customer Get(int id)
        {
            RestClient c = new RestClient();
            c.BaseUrl = customerServiceBaseUrl;

            var request = new RestRequest(id.ToString(), Method.GET);
            var response = c.Execute<Customer>(request);
            var customer = response.Data;
            return customer;
        }
    }
}
