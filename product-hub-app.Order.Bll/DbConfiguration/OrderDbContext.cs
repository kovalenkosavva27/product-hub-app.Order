using Microsoft.EntityFrameworkCore;
using product_hub_app.Order.Contracts.Models;


namespace product_hub_app.Order.Bll.DbConfiguration
{
    public class OrderDbContext: DbContext
    {
        public DbSet<OrderProduct> Products { get; set; }
        public DbSet<Contracts.Models.Order> Orders { get; set; }
        public OrderDbContext(DbContextOptions<OrderDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Contracts.Models.Order>()
                .HasKey(o => o.OrderId);
            builder.Entity<OrderProduct>()
                .HasKey(p => new { p.ProductId, p.OrderId });
            base.OnModelCreating(builder);

        }
    }
}
