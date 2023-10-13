using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace product_hub_app.Order.Contracts.Commands.Dto
{
    public record OrderProductDto(string ProductId, string OrderId, string Name, int Quantity);
}
