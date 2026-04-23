using Microsoft.EntityFrameworkCore;
using EduLearn.Payment.API.Domain.Entities;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit;

namespace EduLearn.Payment.API.Infrastructure.Data;

// DbContext for Payment service — also hosts MassTransit saga state
public class PaymentDbContext : SagaDbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();

    // Register all saga state map configurations
    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new PaymentSagaStateMap(); }
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model); // Applies MassTransit saga tables

        model.Entity<PaymentOrder>(e =>
        {
            e.HasKey(p => p.OrderId);
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            e.Property(p => p.Status).HasMaxLength(20);
        });
    }
}
