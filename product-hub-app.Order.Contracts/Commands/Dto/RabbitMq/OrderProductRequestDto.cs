using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace product_hub_app.Order.Contracts.Commands.Dto.RabbitMq
{
    public record OrderProductRequestDto(string ProductId, int Quantity);
}
