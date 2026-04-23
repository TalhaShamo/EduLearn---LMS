namespace EduLearn.Payment.API.Domain.Entities;

// Stores the payment order record in EduLearnPaymentDb
public class PaymentOrder
{
    public Guid     OrderId          { get; set; } = Guid.NewGuid(); // Saga correlation ID
    public Guid     StudentId        { get; set; }
    public Guid     CourseId         { get; set; }
    public decimal  Amount           { get; set; }
    public string   Currency         { get; set; } = "INR";
    public string   RazorpayOrderId  { get; set; } = string.Empty;  // From Razorpay API
    public string?  RazorpayPaymentId { get; set; }                  // Set on confirmation
    public string   Status           { get; set; } = "Pending";      // Pending→Paid / Failed
    public string?  FailureReason    { get; set; }
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt          { get; set; }
}
