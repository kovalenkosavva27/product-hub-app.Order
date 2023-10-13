using product_hub_app.Order.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace product_hub_app.Order.Contracts.Commands.Dto
{
    public record OrderDto(string OrderId, string CustomerId, OrderStatus Status, DateTime CreatedAt);
}
