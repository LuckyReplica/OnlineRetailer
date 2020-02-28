using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using OrderApi.Data;
using OrderApi.Models;
using RestSharp;

namespace OrderApi.Controllers
{
    [Route("api/Orders")]
    public class OrdersController : Controller
    {
        private readonly IRepository<Order> repository;

        public OrdersController(IRepository<Order> repos)
        {
            repository = repos;
        }

        // GET: api/orders
        [HttpGet]
        public IEnumerable<Order> Get()
        {
            return repository.GetAll();
        }

        // GET api/products/5
        [HttpGet("{id}", Name = "GetOrder")]
        public IActionResult Get(int id)
        {
            var item = repository.Get(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }

        // POST api/orders
        [HttpPost]
        public IActionResult Post([FromBody]Order order)
        {
            if (order == null)
            {
                return BadRequest();
            }

            // Call ProductApi to get the product ordered
            RestClient c = new RestClient();

            // Check customer standing here
            c.BaseUrl = new Uri("https://localhost:52063/customers/");
            var requestCustomer = new RestRequest(order.CustomerID.ToString(), Method.GET);
            var responseCustomer = c.Execute<Customer>(requestCustomer);
            var customer = responseCustomer.Data;

            if (customer == null)
            {
                return BadRequest("Customer could not be found");
            }

            if (customer.CreditStanding)
            {
                // You may need to change the port number in the BaseUrl below
                // before you can run the request.
                c.BaseUrl = new Uri("https://localhost:52063/api/products/");
                var request = new RestRequest(order.ProductId.ToString(), Method.GET);
                var response = c.Execute<Product>(request);
                var orderedProduct = response.Data;

                if (order.Quantity <= orderedProduct.ItemsInStock - orderedProduct.ItemsReserved)
                {
                    // reduce the number of items in stock for the ordered product,
                    // and create a new order.
                    orderedProduct.ItemsReserved += order.Quantity;
                    var updateRequest = new RestRequest(orderedProduct.Id.ToString(), Method.PUT);
                    updateRequest.AddJsonBody(orderedProduct);
                    var updateResponse = c.Execute(updateRequest);

                    if (updateResponse.IsSuccessful)
                    {
                        var newOrder = repository.Add(order);
                        return CreatedAtRoute("GetOrder", new { id = newOrder.Id }, newOrder);
                    }
                }
            }
            else
            {
                return NoContent();
            }

            //// You may need to change the port number in the BaseUrl below
            //// before you can run the request.
            //c.BaseUrl = new Uri("https://localhost:5001/api/products/");
            //var request = new RestRequest(order.ProductId.ToString(), Method.GET);
            //var response = c.Execute<Product>(request);
            //var orderedProduct = response.Data;

            //if (order.Quantity <= orderedProduct.ItemsInStock - orderedProduct.ItemsReserved)
            //{
            //    // reduce the number of items in stock for the ordered product,
            //    // and create a new order.
            //    orderedProduct.ItemsReserved += order.Quantity;
            //    var updateRequest = new RestRequest(orderedProduct.Id.ToString(), Method.PUT);
            //    updateRequest.AddJsonBody(orderedProduct);
            //    var updateResponse = c.Execute(updateRequest);

            //    if (updateResponse.IsSuccessful)
            //    {
            //        var newOrder = repository.Add(order);
            //        return CreatedAtRoute("GetOrder", new { id = newOrder.Id }, newOrder);
            //    }
            //}

            // If the order could not be created, "return no content".

            return NoContent();
        }

    }
}
