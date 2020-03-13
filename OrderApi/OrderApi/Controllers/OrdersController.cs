using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrderApi.Data;
using OrderApi.Infrastructure;
using OrderApi.Models;
using RestSharp;
using SharedModels;

namespace OrderApi.Controllers
{
    [Route("api/Orders")]
    public class OrdersController : Controller
    {
        //This belongs to me /Tomek
        //https://localhost:44382/api/orders
        //https://localhost:44318/Customers
        //https://localhost:44384/api/products

        private readonly IRepository<Models.Order> repository;
        IServiceGateway<Product> productServiceGateway;
        IMessagePublisher messagePublisher;

        public OrdersController(IRepository<Models.Order> repos,
            IServiceGateway<Product> gateway,
            IMessagePublisher publisher)
        {
            repository = repos;
            productServiceGateway = gateway;
            messagePublisher = publisher;
        }

        // GET: api/orders
        [HttpGet]
        public IEnumerable<Models.Order> Get()
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
        public IEnumerable<Models.Order> GetOrderById(int customerId)
        {
            return repository.GetAllByCustomer(customerId);
        }

        [HttpGet]
        [Route("getByProductId/{productId}")]
        public IActionResult GetProductById(int productId)
        {
            var ord = new SharedModels.Order();
            var orderline = new SharedModels.Order.OrderLine { ProductId = 1, OrderId = 1, id = 1, Quantity = 1 };

            ord.OrderLines = new SharedModels.Order.OrderLine[] { orderline };
            if (ProductItemsAvailable(ord))
            {
                return StatusCode(200, "Connection worked");
            }
            return StatusCode(200, "Connection worked");
        }

        [HttpPut]
        [Route("cancelOrder/{orderId}")]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                Models.Order selectedOrder = repository.Get(orderId);

                if (selectedOrder.StatusCode == Models.Order.Status.Shipped)
                {
                    selectedOrder.StatusCode = Models.Order.Status.Cancelled;
                    repository.Edit(selectedOrder);

                    return Ok();
                }
                else
                {
                    return BadRequest("Order could not be cancelled");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPost]
        [Route("GetThroughBus")]
        public IActionResult Post([FromBody]SharedModels.Order order)
        {
            if (order == null)
            {
                return BadRequest();
            }

            if (ProductItemsAvailable(order))
            {
                try
                {
                    // Publish OrderStatusChangedMessage. If this operation
                    // fails, the order will not be created
                     messagePublisher.PublishOrderStatusChangedMessage(
                       order.customerId, order.OrderLines, "completed");

                    // Create order.
                    order.Status = SharedModels.Order.OrderStatus.completed;
                    //var newOrder = repository.Add(order);
                    return CreatedAtRoute("GetOrder", new { id = order.Id }, order);
                }
                catch
                {
                    return StatusCode(500, "An error happened. Try again.");
                }
            }
            else
            {
                // If there are not enough product items available.
                return StatusCode(500, "Not enough items in stock.");
            }
        }

        // POST api/orders
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Models.Order order)
        {
            try
            {
                if (order == null)
                {
                    return BadRequest();
                }

                // Call ProductApi to get the product ordered
                RestClient c = new RestClient();

                // Get customer via http GET call.
                // Check customer standing here
                //c.BaseUrl = new Uri("https://localhost:44318/customers/");
                //var requestCustomer = new RestRequest(order.CustomerID.ToString(), Method.GET);
                //var responseCustomer = c.Execute<SharedModels.Customer>(requestCustomer);
                //var customer = responseCustomer.Data;

                // Get customer via rabbitmq request.
                var customer = messagePublisher.RequestCustomer(order.CustomerID);

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
                        order.StatusCode = Models.Order.Status.Shipped;
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
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        private bool ProductItemsAvailable(SharedModels.Order order)
        {
            foreach (var orderLine in order.OrderLines)
            {
                // Call product service to get the product ordered.
                var orderedProduct = productServiceGateway.Get(orderLine.ProductId);
                if (orderLine.Quantity > orderedProduct.ItemsInStock - orderedProduct.ItemsReserved)
                {
                    return false;
                }
            }
            return true;
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
