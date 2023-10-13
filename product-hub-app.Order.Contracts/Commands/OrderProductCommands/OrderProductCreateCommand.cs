namespace product_hub_app.Order.Contracts.Commands.OrderProductCommands
{
    public record OrderProductCreateCommand(string OrderId, string ProductId, int Quantity);
}
