using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Razorpay.Api;
using EduLearn.Payment.API.Domain.Entities;
using EduLearn.Payment.API.Infrastructure.Data;
using EduLearn.Shared.Events;
using EduLearn.Shared.Models;
using System.Security.Claims;
using System.Text.Json;

namespace EduLearn.Payment.API.Controllers;

[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _db;
    private readonly IPublishEndpoint _bus;
    private readonly IConfiguration   _config;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(PaymentDbContext db, IPublishEndpoint bus,
        IConfiguration config, ILogger<PaymentController> logger)
    { _db = db; _bus = bus; _config = config; _logger = logger; }

    // POST /api/v1/payments/create-order
    // Student clicks "Buy Now" → creates a Razorpay order → returns order ID to Angular
    [HttpPost("create-order")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Create Razorpay order using the test key (dummy payment)
        var rzpClient = new RazorpayClient(
            _config["Razorpay:KeyId"]!,
            _config["Razorpay:KeySecret"]!);

        var options = new Dictionary<string, object>
        {
            { "amount",   (int)(req.Amount * 100) },  // Razorpay uses paise (1 INR = 100 paise)
            { "currency", req.Currency },
            { "receipt",  $"EduLearn-{Guid.NewGuid():N}" }
        };

        var rzpOrder     = rzpClient.Order.Create(options);
        var rzpOrderId   = rzpOrder["id"].ToString()!;
        var correlationId = Guid.NewGuid();  // This becomes the saga's CorrelationId

        // Persist the order record
        var order = new PaymentOrder
        {
            OrderId         = correlationId,
            StudentId       = studentId,
            CourseId        = req.CourseId,
            Amount          = req.Amount,
            Currency        = req.Currency,
            RazorpayOrderId = rzpOrderId
        };
        _db.PaymentOrders.Add(order);
        await _db.SaveChangesAsync();

        // Start the saga by publishing PaymentInitiatedEvent
        await _bus.Publish(new PaymentInitiatedEvent(
            correlationId, studentId, req.CourseId,
            req.Amount, req.Currency, rzpOrderId, DateTime.UtcNow));

        _logger.LogInformation("Payment order {OrderId} created for student {StudentId}", correlationId, studentId);

        // Return the Razorpay order ID and public key to Angular
        // Angular uses these to open the Razorpay checkout popup
        return Ok(ApiResponse<object>.Ok(new
        {
            OrderId         = correlationId,
            RazorpayOrderId = rzpOrderId,
            Amount          = req.Amount,
            Currency        = req.Currency,
            RazorpayKeyId   = _config["Razorpay:KeyId"]
        }));
    }

    // POST /api/v1/payments/verify
    // Called by Angular after Razorpay checkout completes (success or failure)
    // Razorpay sends payment signature — we verify it here
    [HttpPost("verify")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest req)
    {
        var order = await _db.PaymentOrders.FindAsync(req.OrderId);
        if (order is null) return NotFound(ApiResponse<string>.Fail("Order not found."));

        try
        {
            // Verify the Razorpay payment signature (HMAC-SHA256)
            // This confirms the payment is authentic and not tampered
            var attrs = new Dictionary<string, string>
            {
                { "razorpay_order_id",   order.RazorpayOrderId },
                { "razorpay_payment_id", req.RazorpayPaymentId },
                { "razorpay_signature",  req.RazorpaySignature }
            };
            Razorpay.Api.Utils.verifyPaymentSignature(attrs);

            // Signature valid → update order record
            order.RazorpayPaymentId = req.RazorpayPaymentId;
            order.Status            = "Paid";
            order.PaidAt            = DateTime.UtcNow;
            _db.PaymentOrders.Update(order);
            await _db.SaveChangesAsync();

            // Signal the saga to proceed with enrollment
            await _bus.Publish(new PaymentSucceededEvent(
                order.OrderId, order.StudentId, order.CourseId,
                order.Amount, req.RazorpayPaymentId, DateTime.UtcNow));

            return Ok(ApiResponse<string>.Ok("", "Payment verified. Enrollment in progress."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payment signature verification failed for order {OrderId}", req.OrderId);

            order.Status        = "Failed";
            order.FailureReason = "Signature verification failed.";
            _db.PaymentOrders.Update(order);
            await _db.SaveChangesAsync();

            // Signal saga to handle failure
            await _bus.Publish(new PaymentFailedEvent(
                order.OrderId, order.StudentId, order.CourseId,
                "Signature verification failed.", DateTime.UtcNow));

            return BadRequest(ApiResponse<string>.Fail("Payment verification failed."));
        }
    }

    // GET /api/v1/payments/history — student's payment history
    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetHistory()
    {
        var studentId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var orders    = await _db.PaymentOrders
                                 .Where(o => o.StudentId == studentId)
                                 .OrderByDescending(o => o.CreatedAt)
                                 .ToListAsync();

        return Ok(ApiResponse<IEnumerable<PaymentOrder>>.Ok(orders));
    }
}

public record CreateOrderRequest(Guid CourseId, decimal Amount, string Currency = "INR");
public record VerifyPaymentRequest(Guid OrderId, string RazorpayPaymentId, string RazorpaySignature);
