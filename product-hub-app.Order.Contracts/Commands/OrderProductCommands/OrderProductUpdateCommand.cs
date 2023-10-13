namespace product_hub_app.Order.Contracts.Commands.OrderProductCommands
{
    public record OrderProductUpdateCommand(string OrderId, string ProductId, int Quantity);
}
