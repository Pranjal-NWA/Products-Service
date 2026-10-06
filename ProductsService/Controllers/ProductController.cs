using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductsService.DTOs;
using ProductsService.Exceptions;
using ProductsService.Services;

namespace ProductsService.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<PagedProductsResponse>> GetAll(
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _productService.GetAllAsync(search, minPrice, maxPrice, page, pageSize);
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<ProductResponse>> GetById([FromQuery] Guid? id)
    {
        if (id is null || id == Guid.Empty)
            return BadRequest(new { error = "id query parameter is required" });

        try
        {
            var product = await _productService.GetByIdAsync(id.Value);
            return Ok(product);
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request)
    {
        var product = await _productService.CreateAsync(request);
        return StatusCode(201, product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, UpdateProductRequest request)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, request);
            return Ok(product);
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
        catch (ApiException ex)
        {
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}