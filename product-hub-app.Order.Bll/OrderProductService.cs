using Microsoft.EntityFrameworkCore;
using product_hub_app.Order.Bll.DbConfiguration;
using product_hub_app.Order.Contracts.Commands.Dto;
using product_hub_app.Order.Contracts.Commands.OrderProductCommands;
using product_hub_app.Order.Contracts.Interfaces;
using product_hub_app.Order.Contracts.Models;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using product_hub_app.Order.Contracts.Commands.Dto.RabbitMq;
using Microsoft.Extensions.Caching.Distributed;

namespace product_hub_app.Order.Bll
{
    public class OrderProductService:IOrderProductService
    {
        private readonly OrderDbContext _dbContext;

        private readonly IDistributedCache _cache;

        public OrderProductService(OrderDbContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        private async Task<TResponse> SendRabbitMqRequest<TRequest, TResponse>(string queueName, TRequest message)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: queueName,
                               durable: false,
                               exclusive: false,
                               autoDelete: true,
                               arguments: null);

                var correlationId = Guid.NewGuid().ToString();
                var replyQueueName = channel.QueueDeclare().QueueName;

                var props = channel.CreateBasicProperties();
                props.CorrelationId = correlationId;
                props.ReplyTo = replyQueueName;

                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

                channel.BasicPublish(exchange: "",
                               routingKey: queueName,
                               basicProperties: props,
                               body: body);

                var responseEvent = new ManualResetEvent(false);
                string responseMessage = null;

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (ch, ea) =>
                {
                    if (ea.BasicProperties.CorrelationId == correlationId)
                    {
                        responseMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                        responseEvent.Set();
                    }
                };

                channel.BasicConsume(consumer, replyQueueName, true);
                responseEvent.WaitOne();

                return JsonConvert.DeserializeObject<TResponse>(responseMessage);
            }
        }
        public async Task<IEnumerable<OrderProductDto>> GetOrderProducts(string orderId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cachedData = await _cache.GetStringAsync($"OrderProducts_{orderId}", token: cancellationToken);

            if (cachedData != null)
            {
                return JsonConvert.DeserializeObject<IEnumerable<OrderProductDto>>(cachedData);
            }
            else
            {
                var orderProducts = await _dbContext.Products
                    .Where(op => op.OrderId == orderId)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var orderProductDtos = orderProducts.Select(MapToDto);

                var serializedData = JsonConvert.SerializeObject(orderProductDtos);
                await _cache.SetStringAsync($"OrderProducts_{orderId}", serializedData, token: cancellationToken);

                return orderProductDtos;
            }
        }
        public async Task<OrderProductDto> AddProductToOrder(OrderProductCreateCommand createCommand, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == createCommand.OrderId, cancellationToken);
            if (order == null && order.Status != OrderStatus.Processing)
            {
                throw new ArgumentException("Заказ не найден или не доступен");
            }
            var orderProduct = _dbContext.Products.FirstOrDefault(op => op.ProductId == createCommand.ProductId && op.OrderId==createCommand.OrderId);
            if (orderProduct != null)
            {
                throw new ArgumentException("Продукт уже в заказе.");
            }
            var answer =await SendRabbitMqRequest<OrderProductRequestDto, OrderProductCreateResponceDto>(
                "create-queue",
                new OrderProductRequestDto(createCommand.ProductId, createCommand.Quantity));
            if (!answer.IsAvailable)
            {
                throw new ArgumentException("Продукт не найден или запрашиваемое количество больше, чем на складе");
            }

            orderProduct = new OrderProduct
            {
                ProductId = createCommand.ProductId,
                Quantity = createCommand.Quantity,
                OrderId = createCommand.OrderId,
                Name=answer.Name
            };
            _dbContext.Products.Add(orderProduct);
            await _dbContext.SaveChangesAsync(cancellationToken);
            var orderProductDto = MapToDto(orderProduct);
            var serializedData = JsonConvert.SerializeObject(orderProductDto);
            await _cache.SetStringAsync($"OrderProduct_{orderProduct.OrderId}_{orderProduct.ProductId}", serializedData, token: cancellationToken);
            var cachedDataKey = $"OrderProducts_{createCommand.OrderId}";
            var cachedData = await _cache.GetStringAsync(cachedDataKey, token: cancellationToken);

            if (cachedData != null)
            {
                var orderProductDtos = JsonConvert.DeserializeObject<List<OrderProductDto>>(cachedData);
                var newOrderProduct = orderProductDto;
                orderProductDtos.Add(newOrderProduct);
                var updatedData = JsonConvert.SerializeObject(orderProductDtos);
                await _cache.SetStringAsync(cachedDataKey, updatedData, token: cancellationToken);
            }

            return orderProductDto;
        }
        public async Task<OrderProductDto> UpdateOrderProduct(OrderProductUpdateCommand updateCommand, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == updateCommand.OrderId, cancellationToken);
            if (order == null && order.Status != OrderStatus.Processing)
            {
                throw new ArgumentException("Заказ не найден или не доступен.");
            }

            var orderProduct = _dbContext.Products.FirstOrDefault(op => op.ProductId == updateCommand.ProductId && op.OrderId==updateCommand.OrderId);
            if (orderProduct == null)
            {
                throw new ArgumentException("Продукт не найден в заказе.");
            }
            var answer = await SendRabbitMqRequest<OrderProductRequestDto, OrderProductUpdateDeleteResponceDto>(
                "update-queue",
                new OrderProductRequestDto(updateCommand.ProductId, updateCommand.Quantity - orderProduct.Quantity));
            if (!answer.IsAvailable)
            {
                throw new ArgumentException("Продукт не найден или запрашиваемое количество больше, чем на складе");
            }
            orderProduct.Quantity = updateCommand.Quantity;
            await _dbContext.SaveChangesAsync(cancellationToken);
            var orderProductDto = MapToDto(orderProduct);
            var serializedData = JsonConvert.SerializeObject(orderProductDto);
            await _cache.SetStringAsync($"OrderProduct_{orderProduct.OrderId}_{orderProduct.ProductId}", serializedData, token: cancellationToken);
            var cachedDataKey = $"OrderProducts_{updateCommand.OrderId}";
            var cachedData = await _cache.GetStringAsync(cachedDataKey, token: cancellationToken);

            if (cachedData != null)
            {
                var orderProductDtos = JsonConvert.DeserializeObject<List<OrderProductDto>>(cachedData);

                var updatedOrderProductIndex = orderProductDtos.FindIndex(op => op.ProductId == updateCommand.ProductId);

                if (updatedOrderProductIndex != -1)
                {
                    orderProductDtos[updatedOrderProductIndex] = orderProductDto;
                    var updatedData = JsonConvert.SerializeObject(orderProductDtos);
                    await _cache.SetStringAsync(cachedDataKey, updatedData, token: cancellationToken);
                }
            }
            return orderProductDto;
        }
        public async Task<bool> DeleteProductFromOrder(OrderProduсtDeleteCommand deleteCommand, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.OrderId == deleteCommand.OrderId, cancellationToken);
            if (order == null && order.Status != OrderStatus.Processing)
            {
                throw new ArgumentException("Заказ не найден или не доступен.");
            }

            var orderProduct = _dbContext.Products.FirstOrDefault(op => op.ProductId == deleteCommand.ProductId && op.OrderId == deleteCommand.OrderId) ;
            if (orderProduct == null)
            {
                throw new ArgumentException("Продукт не найден в заказе.");
            }
            var answer = await SendRabbitMqRequest<OrderProductRequestDto, OrderProductUpdateDeleteResponceDto>(
                "delete-queue",
                new OrderProductRequestDto(orderProduct.ProductId, orderProduct.Quantity));
            if (!answer.IsAvailable)
            {
                throw new ArgumentException("Продукт не может быть удален");
            }
            _dbContext.Products.Remove(orderProduct);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync($"OrderProduct_{orderProduct.OrderId}_{orderProduct.ProductId}", cancellationToken);
            var cachedDataKey = $"OrderProducts_{deleteCommand.OrderId}";
            var cachedData = await _cache.GetStringAsync(cachedDataKey, token: cancellationToken);

            if (cachedData != null)
            {
                var orderProductDtos = JsonConvert.DeserializeObject<List<OrderProductDto>>(cachedData);
                orderProductDtos.RemoveAll(op => op.ProductId == deleteCommand.ProductId);
                var updatedData = JsonConvert.SerializeObject(orderProductDtos);
                await _cache.SetStringAsync(cachedDataKey, updatedData, token: cancellationToken);
            }

            return true;
        }
        private static OrderProductDto MapToDto(OrderProduct orderProduct)
        {
            return new OrderProductDto(
                orderProduct.ProductId,
                orderProduct.OrderId,
                orderProduct.Name,
                orderProduct.Quantity
            );
        }
    }
}
