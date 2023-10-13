namespace product_hub_app.Order.Contracts.Models
{
    public class Order
    {
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<OrderProduct> Products { get; set; }
    }
    public enum OrderStatus
    {
        Processing,
        Shipped,
        Delivered
    }
}
