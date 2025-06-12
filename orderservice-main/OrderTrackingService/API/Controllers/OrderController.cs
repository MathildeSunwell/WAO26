using Microsoft.AspNetCore.Mvc;
using OrderTrackingService.Application;
using OrderTrackingService.Domain.DTOs;

namespace OrderTrackingService.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController(IOrderService orderService, ILogger<OrderController> logger) : ControllerBase
{
    // POST   /api/orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] OrderDto? orderDto)
    {
        try
        {
            if (orderDto is null)
                return BadRequest("Order payload is required.");
            if (!ModelState.IsValid)
                return ValidationProblem();
            
            var correlationId = Guid.TryParse(HttpContext.Request.Headers["X-Correlation-ID"], out var xCorrelationId) ? xCorrelationId : Guid.NewGuid();
            
            var orderId = await orderService.ProcessCreateOrderAsync(correlationId, orderDto);
            logger.LogInformation("Order created: {orderId}", orderId);

            return Ok($"Order created with OrderID: {orderId}");
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    // GET    /api/orders/{orderId}
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(Guid orderId)
    {
        try
        {
            if (orderId == Guid.Empty)
                return BadRequest("Order ID is required.");
            
            var correlationId = Guid.NewGuid();
            logger.LogInformation("Retrieving order with OrderId: {orderId}", orderId);
            
            var order = await orderService.GetOrderAsync(orderId);
            if (order is null)
                return NotFound($"Order with OrderId {orderId} not found.");

            logger.LogInformation("Order retrieved");
            return Ok(order);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    // GET    /api/orders?customerAddress=&orderStatus=&page=&pageSize= and more
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] OrderQueryParameters query)
    {
        var results = await orderService.GetOrdersAsync(query);
        return Ok(results);
    }
}
