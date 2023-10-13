using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using product_hub_app.Order.Contracts.Commands.Dto;
using product_hub_app.Order.Contracts.Commands.OrderCommands;
using product_hub_app.Order.Contracts.Commands.OrderProductCommands;
using product_hub_app.Order.Contracts.Interfaces;
using System.Diagnostics;
using System.Security.Claims;

namespace product_hub_app.Order.App.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        [Authorize(Policy = "users")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderDto>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(string))]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetAllOrders()
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            var user = HttpContext.User;

            if (user.IsInRole("Director"))
            {
                var orders = await _orderService.GetAllOrders(cancellationToken);
                return Ok(orders);
            }
            else if (user.IsInRole("User"))
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var orders = await _orderService.GetUserOrders(userId, cancellationToken);
                return Ok(orders);
            }
            else
            {
                return Forbid("Нет доступа!");
            }
        }

        [HttpGet("{orderId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<ActionResult<OrderDto>> GetOrderById(string orderId)
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            try
            {
                var order = await _orderService.GetOrderById(orderId, cancellationToken);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("create-order")]
        [Authorize(Roles ="User")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public async Task<IActionResult> CreateOrder()
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            var user = HttpContext.User;
            try
            {
                var createCommand = new OrderCreateCommand
                (
                    user.FindFirstValue(ClaimTypes.NameIdentifier),
                    Contracts.Models.OrderStatus.Processing
                );
                var order = await _orderService.CreateOrder(createCommand, cancellationToken);
                return CreatedAtAction(nameof(GetOrderById), new { orderId = order.OrderId }, order);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update-order/{orderId}")]
        [Authorize(Roles = "Director")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, [FromBody] OrderUpdateCommand updateCommand)
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            try
            {
                var order=await _orderService.UpdateOrderStatus(orderId, updateCommand, cancellationToken);
                return Ok(order);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
