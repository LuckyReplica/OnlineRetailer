using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OrderApi.Data;
using OrderApi.Infrastructure;
using RestSharp;
using SharedModels;

namespace OrderApi.Controllers
{
    [Route("api/Orders")]
    public class OrdersController : Controller
    {
        private readonly IRepository<Order> repository;
        IServiceGateway<Product> productServiceGateway;
        IMessagePublisher messagePublisher;

        public OrdersController(IRepository<Order> repos, IServiceGateway<Product> gateway, IMessagePublisher publisher)
        {
            repository = repos;
            productServiceGateway = gateway;
            messagePublisher = publisher;
        }

        [HttpGet]
        public IEnumerable<Order> Get()
        {
            try
            {
                return repository.GetAll();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpGet]
        [Route("getCustomerById/{id}")]
        public Customer GetCustomer(int id)
        {
            try
            {
                var customer = messagePublisher.RequestCustomer(id);
                return customer;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpGet]
        [Route("getById/{id}")]
        public IActionResult Get(int id)
        {
            try
            {
                var item = repository.Get(id);

                if (item == null)
                {
                    return NotFound("Order could not be found");
                }
                return new ObjectResult(item);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpGet]
        [Route("getByCustomerId/{customerId}")]
        public IEnumerable<Order> GetOrderById(int customerId)
        {
            try
            {
                return repository.GetAllByCustomer(customerId);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPost]
        [Route("CreateOrder")]
        public IActionResult Post([FromBody]Order order)
        {
            if (order == null)
            {
                return BadRequest("Order could not be created");
            }

            if (messagePublisher.IsInStock(order))
            {

                var customer = GetCustomer(order.customerId);

                if (customer == null)
                {
                    return StatusCode(500, "The customer does not exist!");
                }

                if (customer.CreditStanding)
                {
                    try
                    {
                        // Publish OrderStatusChangedMessage. If this operation
                        // fails, the order will not be created
                        messagePublisher.PublishOrderStatusChangedMessage(
                           order.customerId, order.OrderLines, "completed");

                        order.Status = Order.OrderStatus.completed;

                        var newOrder = repository.Add(order);
                        return CreatedAtRoute("GetOrder", newOrder);
                    }
                    catch
                    {
                        return StatusCode(500, "An error happened. Try again.");
                    }
                }
                else
                {
                    return StatusCode(500, "Customer does not have enough resources");
                }
            }
            else
            {
                // If there are not enough product items available.
                return StatusCode(500, "Not enough items in stock.");
            }
        }

        [HttpPut]
        [Route("shipOrder/{orderId}")]
        public IActionResult ShipOrder(int orderId)
        {
            try
            {
                Order selectedOrder = repository.Get(orderId);

                if (selectedOrder == null)
                {
                    return NotFound("Order could not be found");
                }

                if (selectedOrder.Status == Order.OrderStatus.completed)
                {
                    messagePublisher.PublishOrderStatusChangedMessage(
                       selectedOrder.customerId, selectedOrder.OrderLines, "shipped");

                    selectedOrder.Status = Order.OrderStatus.completed;

                    repository.Edit(selectedOrder);
                }
                else
                {
                    return BadRequest("order could not be shipped");
                }

                //send notification to a customer

                return Ok("Order was shipped");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        // if shipped
        [HttpPut]
        [Route("payOrder/{orderId}")]
        public IActionResult PayOrder(int orderId)
        {
            try
            {
                Order selectedOrder = repository.Get(orderId);

                if (selectedOrder == null)
                {
                    return NotFound("Order could not be found");
                }

                if (selectedOrder.Status == Order.OrderStatus.shipped)
                {
                    selectedOrder.Status = Order.OrderStatus.paid;
                    messagePublisher.PublishOrderStatusChangedMessage(selectedOrder.customerId, selectedOrder.OrderLines, "paid");
                        

                    repository.Edit(selectedOrder);

                    return Ok("Transaction was succesful");
                }
                else
                {
                    return BadRequest("The order could not be paid for");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPut]
        [Route("cancelOrder/{orderId}")]
        public IActionResult CancelOrder(int orderId)
        {
            try
            {
                Order selectedOrder = repository.Get(orderId);

                if (selectedOrder == null)
                {
                    return NotFound("Order could not be found");
                }

                if (selectedOrder.Status == Order.OrderStatus.shipped)
                {

                    selectedOrder.Status = Order.OrderStatus.cancelled;

                    repository.Edit(selectedOrder);

                    // no products are already gone
                    //no need to call productapi
                    // return credit standing to a customer?

                    return Ok("Order was cancelled");
                }
                // if has no status which means is created but not yet shipped
                else if (selectedOrder.Status == Order.OrderStatus.completed)
                {
                    messagePublisher.PublishOrderStatusChangedMessage(
                       selectedOrder.customerId, selectedOrder.OrderLines, "cancelled");

                    selectedOrder.Status = Order.OrderStatus.cancelled;

                    repository.Edit(selectedOrder);

                    // return credit standing to a customer?

                    return Ok("Order was cancelled");
                }
                else if (selectedOrder.Status == Order.OrderStatus.paid) {
                    messagePublisher.PublishOrderStatusChangedMessage(
                      selectedOrder.customerId, selectedOrder.OrderLines, "cancelled");

                    selectedOrder.Status = Order.OrderStatus.cancelled;

                    repository.Edit(selectedOrder);

                    return Ok("Order was cancelled");
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


        private bool ProductItemsAvailable(Order order)
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
    }
}
