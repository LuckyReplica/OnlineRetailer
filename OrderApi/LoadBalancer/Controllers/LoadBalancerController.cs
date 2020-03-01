using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using LoadBalancer.Models;
using RestSharp;

namespace LoadBalancer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoadBalancerController : ControllerBase
    {
        string[] url = { "https://localhost:44330/orders/", "https://localhost:44331/orders/", "https://localhost:44332/orders/" };
        int i = 0;

        // GET /orders
        [HttpGet]
        public IEnumerable<Order> Get()
        {
            RestClient c = new RestClient();
            c.BaseUrl = new Uri(url[i % 3]);
            i++;
            var request = new RestRequest(Method.GET);
            var response = c.Execute<List<Order>>(request);
            return response.Data;
        }

        [HttpGet("{id}", Name = "GetOrder")]
        public IActionResult Get(int id)
        {
            RestClient c = new RestClient();
            c.BaseUrl = new Uri(url[i % 3]);
            i++;
            var request = new RestRequest(id.ToString(), Method.GET);
            var response = c.Execute<Order>(request);
            return new ObjectResult(response.Data);
        }

        // POST /orders
        [HttpPost]
        public IActionResult Post([FromBody]Order order)
        {
            if (order == null)
            {
                return BadRequest();
            }

            RestClient c = new RestClient();
            c.BaseUrl = new Uri(url[i % 3]);
            i++;
            var request = new RestRequest(Method.POST);
            request.AddJsonBody(order);
            var response = c.Execute(request);
            return new ObjectResult(response);
        }
    }
}
