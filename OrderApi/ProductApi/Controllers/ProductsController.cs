using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Data;
using SharedModels;

namespace ProductApi.Controllers
{
    [Route("api/Products")]
    public class ProductsController : Controller
    {
        private readonly IRepository<Product> repository;

        public ProductsController(IRepository<Product> repos)
        {
            repository = repos;
        }

        // GET: api/products
        [HttpGet]
        public IEnumerable<Product> Get()
        {
            return repository.GetAll();
        }

        [HttpPut]
        [Route("CheckIfInStock")]
        public IActionResult CheckIfInStock([FromBody]IEnumerable<Order.OrderLine> productsInOrder)
        {
            try
            {
                foreach (var productDTO in productsInOrder)
                {
                    var product = repository.Get(productDTO.ProductId);
                    if (product.ItemsInStock < productDTO.Quantity)
                    {
                        return Ok(false);
                    }
                }
                return Ok(true);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
            
        }

        [HttpPut]
        [Route("ReserveProducts")]
        public IActionResult ReserveProducts([FromBody]IEnumerable<Order.OrderLine> productsInOrder)
        {
            try
            {
                foreach (var productDTO in productsInOrder)
                {
                    var product = repository.Get(productDTO.ProductId);
                    product.ItemsInStock -= productDTO.Quantity;
                    product.ItemsReserved += productDTO.Quantity;
                    repository.Edit(product);                    
                }
                return Ok();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
            
        }

        [HttpGet("{id}", Name="GetProduct")]
        public IActionResult Get(int id)
        {
            try
            {
                var item = repository.Get(id);

                if (item == null)
                {
                    return NotFound("Product could not be found");
                }

                return new ObjectResult(item);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPost]
        public IActionResult Post([FromBody]Product product)
        {
            try
            {
                if (product == null)
                {
                    return BadRequest("Product could not be created");
                }

                var newProduct = repository.Add(product);

                return CreatedAtRoute("GetProduct", new { id = newProduct.Id }, newProduct);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody]Product product)
        {
            try
            {
                if (product == null || product.Id != id)
                {
                    return BadRequest("Product could not be updated");
                }

                var modifiedProduct = repository.Get(id);

                if (modifiedProduct == null)
                {
                    return NotFound("Produck was not found");
                }

                repository.Edit(modifiedProduct);

                return StatusCode(204);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                if (repository.Get(id) == null)
                {
                    return NotFound();
                }

                repository.Remove(id);

                return StatusCode(410);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException != null ? ex.Message + ex.InnerException : ex.Message);
            }
        }
    }
}
