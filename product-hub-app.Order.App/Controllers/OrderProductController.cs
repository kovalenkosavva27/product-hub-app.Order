using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using product_hub_app.Order.Contracts.Commands.Dto;
using product_hub_app.Order.Contracts.Commands.OrderProductCommands;
using product_hub_app.Order.Contracts.Interfaces;

namespace product_hub_app.Order.App.Controllers
{
    [ApiController]
    [Route("api/orders/products")]
    public class OrderProductController : ControllerBase
    {
        private readonly IOrderProductService _orderProductService;

        public OrderProductController(IOrderProductService orderProductService)
        {
            _orderProductService = orderProductService;
        }

        [HttpGet("{orderId}/products")]
        [Authorize(Policy = "users")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderProductDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<ActionResult<IEnumerable<OrderProductDto>>> GetOrderProducts(string orderId)
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            var orderProducts = await _orderProductService.GetOrderProducts(orderId, cancellationToken);
            return Ok(orderProducts);
        }

        [HttpPost("add-product")]
        [Authorize(Roles ="User")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OrderProductDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
        public async Task<IActionResult> AddProductToOrder([FromBody] OrderProductCreateCommand createCommand)
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            try
            {
                var orderProduct = await _orderProductService.AddProductToOrder(createCommand, cancellationToken);
                return CreatedAtAction(nameof(GetOrderProducts), new { createCommand.OrderId }, orderProduct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update-product")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderProductDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<IActionResult> UpdateOrderProduct([FromBody] OrderProductUpdateCommand updateCommand)
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            try
            {
                var orderProduct = await _orderProductService.UpdateOrderProduct(updateCommand, cancellationToken);
                return Ok(orderProduct);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }
        
        [HttpDelete("delete-product")]
        [Authorize(Roles = "User")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(string))]
        public async Task<IActionResult> DeleteProductFromOrder([FromBody] OrderProduсtDeleteCommand deleteCommand)
        {
            var cancellationToken = HttpContext?.RequestAborted ?? default;
            try
            {
                var isDeleted = await _orderProductService.DeleteProductFromOrder(deleteCommand, cancellationToken);
                if (isDeleted)
                {
                    return NoContent();
                }
                else
                {
                    return NotFound("Продукт не найден в заказе.");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
