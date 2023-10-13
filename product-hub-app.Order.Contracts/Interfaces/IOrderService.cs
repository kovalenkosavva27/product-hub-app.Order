using product_hub_app.Order.Contracts.Commands.Dto;
using product_hub_app.Order.Contracts.Commands.OrderCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace product_hub_app.Order.Contracts.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrder(OrderCreateCommand createCommand, CancellationToken cancellationToken = default);
        Task<OrderDto> GetOrderById(string orderId, CancellationToken cancellationToken = default);
        Task<OrderDto> UpdateOrderStatus(string orderId, OrderUpdateCommand updateCommand, CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDto>> GetAllOrders(CancellationToken cancellationToken = default);
        Task<IEnumerable<OrderDto>> GetUserOrders(string customerId, CancellationToken cancellationToken = default);
    }
}
