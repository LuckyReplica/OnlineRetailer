using OrderApi.Models;
using RestSharp;
using SharedModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderApi.Infrastructure
{
    public class ProductsServiceGateway : IServiceGateway<Product>
    {
        Uri productServiceBaseUrl;

        public ProductsServiceGateway(Uri baseUrl)
        {
            productServiceBaseUrl = baseUrl;
        }

        public Product Get(int id)
        {
            RestClient c = new RestClient();
            c.BaseUrl = productServiceBaseUrl;

            var request = new RestRequest(id.ToString(), Method.GET);
            var response = c.Execute<Product>(request);
            var orderedProduct = response.Data;
            return orderedProduct;
        }
    }
}
