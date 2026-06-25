# Authentication Architecture — Customer Portal

**Document type:** Engineering Specification  
**Service:** `auth-service` (ASP.NET Core 8)  
**Last updated:** 2026-03-15  
**Owner:** Platform Identity Team

---

## Overview

The customer portal uses a **server-side session** model backed by Redis. The React SPA stores only a session reference cookie (`.AspNetCore.Session`). Password validation, MFA policy, and session lifecycle are owned by `auth-service`.

---

## Login flow

1. User submits credentials to `POST /api/auth/login`
2. Auth service validates password (ASP.NET Identity + lockout policy)
3. On success, creates session in Redis with TTL = 30 minutes (sliding)
4. Returns session cookie to browser
5. SPA calls `GET /api/auth/me` to load user profile

---

## Logout flow (intended behavior)

1. Client calls `POST /api/auth/logout` with credentials cookie
2. Server removes session key from Redis
3. Server clears session cookie in response
4. Client redirects to `/login`

**Important:** Client-side navigation to `/login` without calling the logout API does **not** invalidate the session. Any code path that handles "Log out" must invoke the API before redirect.

---

## Session configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Idle timeout | 30 minutes | Sliding expiration on each authenticated request |
| Absolute max | 8 hours | Hard limit even with activity |
| Storage | Redis cluster `identity-sessions` | Key pattern `sess:{userId}:{sessionId}` |
| Cookie | `.AspNetCore.Session` | HttpOnly, Secure, SameSite=Lax |

---

## Password policy

- Minimum 12 characters
- Lockout after 10 failed attempts (15-minute lock)
- Password reset via email link (1-hour token)

---

## MFA status

- **Current:** Password only for all users
- **Planned:** OTP via SMS (see requirement REQ-2026-0412)
- Admin policy engine stub exists but is not exposed in UI

---

## Related services

| Service | Integration |
|---------|-------------|
| `notification-service` | Email for password reset; Twilio SMS for alerts |
| `audit-service` | All auth events (`LOGIN_SUCCESS`, `LOGIN_FAIL`, `LOGOUT`, `LOCKOUT`) |
| `customer-api` | Validates session via shared auth middleware |

---

## Troubleshooting runbook (excerpt)

### User "still logged in" after logout

1. Confirm browser sent `POST /api/auth/logout` (check auth-service access logs)
2. If only client redirect occurred, session remains in Redis until idle timeout
3. Verify frontend menu component calls `authApi.logout()` — regression introduced in SPA routing refactor PR #1847
4. Manual remediation: delete Redis key `sess:{userId}:{sessionId}` or wait for TTL

### Session expires too quickly

- Check Redis memory eviction policy
- Verify sliding expiration middleware is registered before controllers

---

## API reference (auth-service)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/auth/login` | POST | Authenticate, create session |
| `/api/auth/logout` | POST | Invalidate session, clear cookie |
| `/api/auth/me` | GET | Current user profile |
| `/api/auth/refresh` | POST | Extend sliding session |

---

## Security notes

- Never log raw passwords or OTP codes
- Rate-limit login and OTP endpoints per IP and per account
- All auth endpoints require HTTPS in production
