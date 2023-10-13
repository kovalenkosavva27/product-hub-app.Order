using product_hub_app.Order.Contracts.Commands.Dto;
using product_hub_app.Order.Contracts.Commands.OrderProductCommands;

namespace product_hub_app.Order.Contracts.Interfaces
{
    public interface IOrderProductService
    {
        Task<IEnumerable<OrderProductDto>> GetOrderProducts(string orderId, CancellationToken cancellationToken = default);

        Task<OrderProductDto> AddProductToOrder(OrderProductCreateCommand createCommand, CancellationToken cancellationToken = default);

        Task<OrderProductDto> UpdateOrderProduct(OrderProductUpdateCommand updateCommand, CancellationToken cancellationToken = default);

        Task<bool> DeleteProductFromOrder(OrderProduсtDeleteCommand deleteCommand, CancellationToken cancellationToken = default);
    }
    public record RabbitMQTestProduct(string ProductId, int Quantity);
    public record RabbitMQTestAnswer(bool IsAvailable);
}
