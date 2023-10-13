namespace product_hub_app.Order.Contracts.Models
{
    public class OrderProduct
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public string Name { get; set; }
        public string OrderId { get; set; }
        public Order Order { get; set; }
    }
}
