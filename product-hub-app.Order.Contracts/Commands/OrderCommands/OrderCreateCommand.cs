using product_hub_app.Order.Contracts.Models;

namespace product_hub_app.Order.Contracts.Commands.OrderCommands
{
    public record OrderCreateCommand(string CustomerId, OrderStatus Status);
}
