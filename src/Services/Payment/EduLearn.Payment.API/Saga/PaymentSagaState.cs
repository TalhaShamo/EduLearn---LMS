using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EduLearn.Payment.API.Infrastructure.Data;

// MassTransit saga state — the orchestrator's memory between steps
// Persisted to EduLearnPaymentDb so it survives service restarts
public class PaymentSagaState : SagaStateMachineInstance
{
    // Required by MassTransit: unique saga instance identifier = OrderId
    public Guid CorrelationId  { get; set; }

    // MassTransit state name (e.g., "Initial", "PaymentPending", "Completed")
    public string CurrentState { get; set; } = string.Empty;

    // Business data stored across saga steps
    public Guid    StudentId       { get; set; }
    public Guid    CourseId        { get; set; }
    public decimal Amount          { get; set; }
    public string  RazorpayOrderId { get; set; } = string.Empty;
    public string? RazorpayPaymentId { get; set; }
    public int     RetryCount      { get; set; }  // Track retries
}

// EF Core mapping for the saga state table
public class PaymentSagaStateMap : SagaClassMap<PaymentSagaState>
{
    protected override void Configure(EntityTypeBuilder<PaymentSagaState> entity, ModelBuilder model)
    {
        entity.Property(s => s.CurrentState).HasMaxLength(64);
        entity.Property(s => s.RazorpayOrderId).HasMaxLength(100);
        entity.Property(s => s.Amount).HasColumnType("decimal(18,2)");
    }
}
