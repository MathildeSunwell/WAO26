using Microsoft.AspNetCore.Mvc;

namespace PaymentService.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    // Payment service is purely event-driven via RabbitMQ
    // No HTTP endpoints needed for payment processing
    
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Payment service is running", timestamp = DateTime.UtcNow });
    }
}