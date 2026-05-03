import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Course, CreateOrderResponse } from '../../core/models/models';
import { AuthService } from '../../core/services/auth.service';

declare var Razorpay: any;

@Component({
  selector: 'app-checkout',
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.scss']
})
export class CheckoutComponent implements OnInit, OnDestroy {
  course: Course | null = null;
  isLoading = true;
  isProcessing = false;
  paymentError = '';
  private courseId = '';
  private destroy$ = new Subject<void>();

  constructor(
    private http: HttpClient,
    private route: ActivatedRoute,
    private router: Router,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(p => {
      this.courseId = p['courseId'];
      this.loadCourse();
    });
    this.loadRazorpayScript();
  }

  loadCourse(): void {
    this.http.get<any>(`${environment.apiUrl}/courses/${this.courseId}`)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => { 
          this.course = res?.data || res; 
          this.isLoading = false; 
        },
        error: () => {
          this.isLoading = false;
          this.paymentError = 'Failed to load course details. Please try again.';
        }
      });
  }

  loadRazorpayScript(): void {
    if (document.getElementById('rzp-script')) return;
    const script = document.createElement('script');
    script.id = 'rzp-script';
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    document.body.appendChild(script);
  }

  onPayNow(): void {
    this.isProcessing = true;
    this.paymentError = '';
    const user = this.authService.currentUser!;

    this.http.post<any>(`${environment.apiUrl}/payments/create-order`, {
      courseId: this.course!.courseId,
      amount: this.course!.price,
      currency: 'INR'
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          const order = res?.data || res;
          this.openRazorpay(order, user);
        },
        error: (err) => {
          this.isProcessing = false;
          this.paymentError = err?.error?.message ?? 'Could not initiate payment. Please try again.';
        }
      });
  }

  openRazorpay(order: CreateOrderResponse, user: any): void {
    const options = {
      key: order.razorpayKeyId,
      amount: order.amount * 100,
      currency: 'INR',
      name: 'EduLearn',
      description: this.course!.title,
      order_id: order.razorpayOrderId,
      prefill: { name: user.fullName, email: user.email },
      theme: { color: '#3949AB' },
      handler: (response: any) => this.verifyPayment(order.orderId, response),
      modal: { ondismiss: () => { this.isProcessing = false; } }
    };

    const rzp = new Razorpay(options);
    rzp.on('payment.failed', (resp: any) => {
      this.isProcessing = false;
      this.paymentError = 'Payment failed: ' + resp.error.description;
    });
    rzp.open();
  }

  verifyPayment(orderId: string, rzpResponse: any): void {
    this.http.post(`${environment.apiUrl}/payments/verify`, {
      orderId,
      razorpayPaymentId: rzpResponse.razorpay_payment_id,
      razorpaySignature: rzpResponse.razorpay_signature
    }).pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.router.navigate(['/learn', this.course!.courseId], { queryParams: { enrolled: '1' } }),
        error: () => {
          this.isProcessing = false;
          this.paymentError = 'Payment verification failed. Contact support.';
        }
      });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }
}
