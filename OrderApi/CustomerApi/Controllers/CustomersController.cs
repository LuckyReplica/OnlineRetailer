using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CustomerApi.Data;
using SharedModels;
using System;

namespace CustomerApi.Controllers
{
    [ApiController]
    [Route("Customers")]
    public class CustomersController : ControllerBase
    {
        private readonly IRepository<Customer> repository;

        public CustomersController(IRepository<Customer> repos)
        {
            repository = repos;
        }

        [HttpGet]
        public IEnumerable<Customer> Get()
        {

            return repository.GetAll();
        }

        [HttpGet("{id}", Name = "GetCustomer")]
        public IActionResult Get(int id)
        {
            try
            {
                var item = repository.Get(id);

                if (item == null)
                {
                    return NotFound("Customer was not found");
                }

                return Ok(new ObjectResult(item));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPost]
        public IActionResult Post([FromBody]Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    return BadRequest("Customer could not be created");
                }

                var newCustomer = repository.Add(customer);

                return CreatedAtRoute("GetCustomer", new { id = newCustomer.Id }, newCustomer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPut]
        public IActionResult Edit([FromBody]Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    return BadRequest("Customer could not be edited");
                }

                repository.Edit(customer);

                return StatusCode(204);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Remove(int id)
        {
            try
            {
                if (id == 0)
                {
                    return BadRequest("Customer could not be removed");
                }

                repository.Remove(id);

                return StatusCode(204);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }
    }
}
