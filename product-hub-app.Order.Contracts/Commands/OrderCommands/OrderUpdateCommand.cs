using product_hub_app.Order.Contracts.Models;

namespace product_hub_app.Order.Contracts.Commands.OrderCommands
{
    public record OrderUpdateCommand(OrderStatus Status);
}
