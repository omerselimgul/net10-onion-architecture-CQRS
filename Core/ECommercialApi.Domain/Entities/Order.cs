using ECommercialApi.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommercialApi.Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public ICollection<Product> Products { get; set; }
        public Customer Customer { get; set; }
    }

    public class OrderConfiguration : BaseEntityConfiguration<Order>
    {
        public override void Configure(EntityTypeBuilder<Order> builder)
        {
            base.Configure(builder);
            builder.ToTable("Orders");


            builder.Property(x => x.Description)
                   .HasMaxLength(500);

            builder.Property(x => x.Address)
                   .IsRequired()
                   .HasMaxLength(300);

            // Customer ile ilişki (One-to-Many)
            builder.HasOne(x => x.Customer)
                   .WithMany(c => c.Orders)
                   .HasForeignKey(x => x.CustomerId);

            // Product ile ilişki (Many-to-Many)
            builder.HasMany(x => x.Products)
                   .WithMany(p => p.Orders);
        }
    }
}