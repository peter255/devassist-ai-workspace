# Order Processing Service — Overview

**Document type:** Engineering Specification  
**Service:** `order-service`  
**Last updated:** 2026-02-20

---

## Purpose

The order processing service orchestrates checkout: cart validation, inventory reservation, payment capture, and fulfillment handoff.

---

## Happy path

1. `POST /api/orders` — create order from cart (status: `PendingPayment`)
2. Inventory reserved for 15 minutes via `inventory-service`
3. `payment-service` charges customer
4. On success: status → `Paid`, event `OrderPaid` published
5. `fulfillment-service` picks up order within 2 minutes (avg)

---

## Payment failure handling

When `payment-service` returns a non-retryable decline:
- Order status → `PaymentFailed`
- Inventory reservation released immediately
- User sees error with retry option (new payment attempt creates new `paymentIntentId`)

When payment times out (>30s):
- Order remains `PendingPayment` for 5 minutes
- Background job `PaymentReconciliationJob` polls payment status
- If still unknown after 5 minutes, order cancelled and inventory released

**Alert:** If payment failure rate exceeds 5% in 10 minutes, PagerDuty incident `ORDER-PAYMENT-SPIKE` fires.

---

## Idempotency

- Clients must send `Idempotency-Key` header on order creation
- Duplicate keys within 24 hours return the original order response

---

## Dependencies

| Service | Failure mode |
|---------|--------------|
| `inventory-service` | Checkout blocked; 503 to user |
| `payment-service` | Retryable errors allow 2 automatic retries |
| `fulfillment-service` | Order stays `Paid` until manual replay |

---

## Observability

- Trace ID propagated from API gateway
- Key metrics: `orders_created`, `payment_failures`, `checkout_latency_p95`
