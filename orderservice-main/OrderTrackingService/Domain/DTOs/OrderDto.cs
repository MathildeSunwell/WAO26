namespace OrderTrackingService.Domain.DTOs;

public class OrderDto
{
    public string CustomerAddress { get; set; }
    public required decimal TotalPrice { get; set; }
    public required List<OrderItem> OrderItems { get; set; } = new();
}

public class OrderItem
{
    public required string ProductName { get; set; }
    public required string Size { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}