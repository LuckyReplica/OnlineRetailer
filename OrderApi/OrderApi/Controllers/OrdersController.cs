using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrderApi.Data;
using OrderApi.Models;
using RestSharp;

namespace OrderApi.Controllers
{
    [Route("api/Orders")]
    public class OrdersController : Controller
    {
        //This belongs to me /Tomek
        //https://localhost:44382/api/orders
        //https://localhost:44318/Customers
        //https://localhost:44384/api/products

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
        [HttpGet]
        [Route("getById/{id}")]
        public IActionResult Get(int id)
        {
            var item = repository.Get(id);
            if (item == null)
            {
                return NotFound();
            }
            return new ObjectResult(item);
        }

        [HttpGet]
        [Route("getByCustomerId/{customerId}")]
        public IEnumerable<Order> GetOrderById(int customerId)
        {
            return repository.GetAllByCustomer(customerId);
        }

        [HttpPut]
        [Route("cancelOrder/{orderId}")]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                Order selectedOrder = repository.Get(orderId);

                if (selectedOrder.StatusCode == Order.Status.Shipped)
                {
                    selectedOrder.StatusCode = Order.Status.Cancelled;
                    repository.Edit(selectedOrder);
              
                    return Ok();
                }
                else
                {
                    return BadRequest("Order could not be cancelled");
                }             
            }
            catch(Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }   
        }

        // POST api/orders
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Order order)
        {
            try
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
                    var areProductsAvailable = await CheckIfProductsAreInStock(order.Products);
                    if (areProductsAvailable == "true")
                    {
                        var wasSuccesfull = await addItemsToReserved(order.Products);
                        order.StatusCode = Order.Status.Shipped;
                        repository.Add(order);

                        return Ok();
                    }
                    else
                    {
                        return BadRequest("Not enough items in stock");
                    }
                }
                else
                {
                    return BadRequest("Customer does not have resources to make a purchase");
                }
            }
            catch(Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        private async Task<String> CheckIfProductsAreInStock(IEnumerable<ProductDTO> listOfProducts)
        {
            var json = JsonConvert.SerializeObject(listOfProducts);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var url = "https://localhost:44384/api/products/CheckIfInStock";
            HttpClient client = new HttpClient();
            var response = await client.PutAsync(url, data);
            return response.Content.ReadAsStringAsync().Result;          
        }
        private async Task<String> addItemsToReserved(IEnumerable<ProductDTO> listOfProducts)
        {
            var json = JsonConvert.SerializeObject(listOfProducts);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var url = "https://localhost:44384/api/products/ReserveProducts";
            HttpClient client = new HttpClient();
            var response = await client.PutAsync(url, data);
            return response.Content.ReadAsStringAsync().Result;
        }
    }
}
