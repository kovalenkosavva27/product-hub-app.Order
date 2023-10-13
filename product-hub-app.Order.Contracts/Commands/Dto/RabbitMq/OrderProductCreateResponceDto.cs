namespace product_hub_app.Order.Contracts.Commands.Dto.RabbitMq
{
    public record OrderProductCreateResponceDto(string Name, bool IsAvailable);
}
