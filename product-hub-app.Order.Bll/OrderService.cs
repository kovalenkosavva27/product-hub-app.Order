using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using product_hub_app.Order.Bll.DbConfiguration;
using product_hub_app.Order.Contracts.Commands.Dto;
using product_hub_app.Order.Contracts.Commands.OrderCommands;
using product_hub_app.Order.Contracts.Commands.OrderProductCommands;
using product_hub_app.Order.Contracts.Interfaces;
using product_hub_app.Order.Contracts.Models;
namespace product_hub_app.Order.Bll
{
    public class OrderService: IOrderService
    {
        private readonly OrderDbContext _dbContext;
        private readonly IDistributedCache _cache;

        public OrderService(OrderDbContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<OrderDto> CreateOrder(OrderCreateCommand createCommand, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order = new Contracts.Models.Order
            {
                OrderId= Guid.NewGuid().ToString(),
                CustomerId = createCommand.CustomerId,
                Status = createCommand.Status,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync(cancellationToken);
            var orderDto = MapToDto(order);
            var serializedData = JsonConvert.SerializeObject(orderDto);
            await _cache.SetStringAsync($"Order_{order.OrderId}", serializedData, token: cancellationToken);
            return orderDto;
        }

        public async Task<OrderDto> GetOrderById(string orderId, CancellationToken cancellationToken = default)
        {

            var cachedData = await _cache.GetStringAsync($"Order_{orderId}", token: cancellationToken);

            if (cachedData != null)
            {
                return JsonConvert.DeserializeObject<OrderDto>(cachedData);
            }
            else
            {
                var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

                if (order == null)
                {
                    throw new ArgumentException("Заказ не найден.");
                }

                var orderDto = MapToDto(order);

                var serializedData = JsonConvert.SerializeObject(orderDto);
                await _cache.SetStringAsync($"Order_{orderId}", serializedData, token: cancellationToken);

                return orderDto;
            }
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrders(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedData = await _cache.GetStringAsync("AllOrders", token: cancellationToken);

            if (cachedData != null)
            {
                return JsonConvert.DeserializeObject<IEnumerable<OrderDto>>(cachedData);
            }
            else
            {
                var orders = await _dbContext.Orders.AsNoTracking().ToListAsync(cancellationToken);
                var orderDtos = orders.Select(MapToDto);

                var serializedData = JsonConvert.SerializeObject(orderDtos);
                await _cache.SetStringAsync("AllOrders", serializedData, token: cancellationToken);

                return orderDtos;
            }
        }
        public async Task<IEnumerable<OrderDto>> GetUserOrders(string customerId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cacheKey = $"UserOrders_{customerId}";
            var cachedData = await _cache.GetStringAsync(cacheKey, token: cancellationToken);

            if (cachedData == null)
            {
                var userOrders = await _dbContext.Orders
                    .Where(order => order.CustomerId == customerId)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var orderDtos = userOrders.Select(MapToDto);

                var serializedData = JsonConvert.SerializeObject(orderDtos);
                await _cache.SetStringAsync(cacheKey, serializedData, token: cancellationToken);

                return orderDtos;
            }

            return JsonConvert.DeserializeObject<IEnumerable<OrderDto>>(cachedData);
        }

        public async Task<OrderDto> UpdateOrderStatus(string orderId, OrderUpdateCommand updateCommand, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);
            if (order == null)
            {
                throw new ArgumentException("Заказ не найден.");
            }

            order.Status = updateCommand.Status;
            await _dbContext.SaveChangesAsync(cancellationToken);
            var orderDto = MapToDto(order);
            var serializedData = JsonConvert.SerializeObject(orderDto);
            await _cache.SetStringAsync($"Order_{order.OrderId}", serializedData, token: cancellationToken);
            var cachedData = await _cache.GetStringAsync("AllOrders", token: cancellationToken);
            if (cachedData != null)
            {
                var orderDtos = JsonConvert.DeserializeObject<List<OrderDto>>(cachedData);

                var updatedOrderIndex = orderDtos.FindIndex(o => o.OrderId == orderDto.OrderId);

                if (updatedOrderIndex != -1)
                {
                    orderDtos[updatedOrderIndex] = orderDto;
                    var updatedData = JsonConvert.SerializeObject(orderDtos);
                    await _cache.SetStringAsync("AllOrders", updatedData, token: cancellationToken);
                }
            }
            return orderDto;
        }

        private static OrderDto MapToDto(Contracts.Models.Order order)
        {
            return new OrderDto(
                order.OrderId,
                order.CustomerId,
                order.Status,
                order.CreatedAt
            );
        }
    }
}
