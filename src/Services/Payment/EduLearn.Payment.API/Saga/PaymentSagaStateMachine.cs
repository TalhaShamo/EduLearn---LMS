using MassTransit;
using EduLearn.Shared.Events;

namespace EduLearn.Payment.API.Saga;

// ── PAYMENT SAGA STATE MACHINE ────────────────────────────────
// Orchestrates: InitiatePayment → [Razorpay Confirm/Fail] → Enroll / Notify
// Uses MassTransit StateMachine (Orchestration pattern)
public class PaymentSagaStateMachine : MassTransitStateMachine<PaymentSagaState>
{
    // States the saga can be in
    public State PaymentPending  { get; private set; } = null!;
    public State PaymentComplete { get; private set; } = null!;
    public State PaymentFailed   { get; private set; } = null!;

    // Events that trigger state transitions
    public Event<PaymentInitiatedEvent>   PaymentInitiated  { get; private set; } = null!;
    public Event<PaymentSucceededEvent>   PaymentSucceeded  { get; private set; } = null!;
    public Event<PaymentFailedEvent>      PaymentFailed_Evt { get; private set; } = null!;

    public PaymentSagaStateMachine()
    {
        // Tell MassTransit which property holds the current state name
        InstanceState(x => x.CurrentState);

        // Map each event's correlation ID to the saga's CorrelationId
        Event(() => PaymentInitiated,  e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentSucceeded,  e => e.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentFailed_Evt, e => e.CorrelateById(m => m.Message.OrderId));

        // ── WORKFLOW ────────────────────────────────────────────

        // Step 1: Client initiates payment → saga saves context and moves to Pending
        Initially(
            When(PaymentInitiated)
                .Then(ctx =>
                {
                    // Save payment details into saga state (durable memory)
                    ctx.Saga.StudentId        = ctx.Message.StudentId;
                    ctx.Saga.CourseId         = ctx.Message.CourseId;
                    ctx.Saga.Amount           = ctx.Message.Amount;
                    ctx.Saga.RazorpayOrderId  = ctx.Message.RazorpayOrderId;
                })
                .TransitionTo(PaymentPending));

        // Step 2a: Razorpay webhook confirms payment → publish success → enroll student
        During(PaymentPending,
            When(PaymentSucceeded)
                .Then(ctx => ctx.Saga.RazorpayPaymentId = ctx.Message.RazorpayPaymentId)
                .Publish(ctx => new PaymentSucceededEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.StudentId,
                    ctx.Saga.CourseId,
                    ctx.Saga.Amount,
                    ctx.Saga.RazorpayPaymentId!,
                    DateTime.UtcNow))
                .TransitionTo(PaymentComplete)
                .Finalize());  // Mark saga complete — removes from saga state table

        // Step 2b: Payment fails → publish failure event → notify student
        During(PaymentPending,
            When(PaymentFailed_Evt)
                .Publish(ctx => new PaymentFailedEvent(
                    ctx.Saga.CorrelationId,
                    ctx.Saga.StudentId,
                    ctx.Saga.CourseId,
                    ctx.Message.Reason,
                    DateTime.UtcNow))
                .TransitionTo(PaymentFailed)
                .Finalize());

        // Automatically delete saga instance from DB when finalized
        SetCompletedWhenFinalized();
    }
}
