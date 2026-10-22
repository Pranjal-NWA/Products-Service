using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductsService.DTOs;
using ProductsService.Exceptions;
using ProductsService.Models;
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

    // ---------- Browsing ----------

    [HttpGet("all")]
    public async Task<ActionResult<PagedProductsResponse>> GetAll([FromQuery] ProductQuery query)
    {
        if (!User.IsInRole("Admin"))
        {
            query.IsActive = null;
            query.IncludeInactive = false;
        }
        return Ok(await _productService.GetAllAsync(query));
    }
    [HttpGet]
    public async Task<ActionResult<ProductResponse>> GetById([FromQuery] Guid? id)
    {
        if (id is null || id == Guid.Empty)
            return BadRequest(new { error = "id query parameter is required" });

        try { return Ok(await _productService.GetByIdAsync(id.Value)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<CategoryNode>>> Categories()
        => Ok(await _productService.GetCategoryTreeAsync());

    [HttpGet("series")]
    public async Task<ActionResult<List<SeriesResponse>>> SeriesList(
        [FromQuery] ProductCategory? category, [FromQuery] string? subcategory)
        => Ok(await _productService.GetSeriesAsync(category, subcategory));

    [HttpGet("series/{id:guid}")]
    public async Task<ActionResult<SeriesDetailResponse>> SeriesDetail(Guid id)
    {
        try { return Ok(await _productService.GetSeriesByIdAsync(id)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    // ---------- Admin: series ----------

    [Authorize(Roles = "Admin")]
    [HttpPost("series")]
    public async Task<ActionResult<SeriesResponse>> CreateSeries(SeriesRequest request)
    {
        try { return StatusCode(201, await _productService.CreateSeriesAsync(request)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("series/{id:guid}")]
    public async Task<ActionResult<SeriesResponse>> UpdateSeries(Guid id, UpdateSeriesRequest request)
    {
        try { return Ok(await _productService.UpdateSeriesAsync(id, request)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("series/{id:guid}")]
    public async Task<IActionResult> DeleteSeries(Guid id)
    {
        try { await _productService.DeleteSeriesAsync(id); return NoContent(); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("series/{id:guid}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<SeriesResponse>> UploadSeriesImage(Guid id, IFormFile file, CancellationToken ct)
    {
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _productService.UploadSeriesImageAsync(id, stream, file.Length, ct));
        }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    // ---------- Admin: orderable rows ----------

    [Authorize(Roles = "Admin")]
    [HttpPost("series/{id:guid}/products")]
    public async Task<ActionResult<ProductResponse>> AddProduct(Guid id, ProductRequest request)
    {
        try { return StatusCode(201, await _productService.AddProductAsync(id, request)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(Guid id, UpdateProductRequest request)
    {
        try { return Ok(await _productService.UpdateProductAsync(id, request)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        try { await _productService.DeleteProductAsync(id); return NoContent(); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("bulk")]
    public async Task<ActionResult<BulkCreateResult>> BulkCreate(List<BulkCreateProductRequest> requests)
    {
        try { return StatusCode(201, await _productService.BulkCreateAsync(requests)); }
        catch (ApiException ex) { return StatusCode(ex.StatusCode, new { error = ex.Message }); }
    }
}