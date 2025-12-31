using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApi.Controllers;

[Authorize]
[Produces("application/json")]
[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> Get()
    {
        return Ok(new List<Product>
        {
            new Product { Code = "P001", Name = "Product 1", Description = "Description 1" },
            new Product { Code = "P002", Name = "Product 2", Description = "Description 2" },
            new Product { Code = "P003", Name = "Product 3", Description = "Description 3" }
        });
    }
}

public class Product
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }
}