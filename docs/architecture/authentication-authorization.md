# Authentication and Authorization

JWT validation checks issuer, audience, lifetime, signing key, user existence, active/deleted status, a current staff role, and lockout. Password login and refresh also reject inactive/deleted users and users without a current staff role. JWT and refresh validation do not check security stamps, so password changes alone do not invalidate existing sessions.

Identity roles are `Superadmin` for staff provisioning and role administration, `Admin` for book catalog management, and `Officer` for membership and lending operations. Members do not have Identity accounts. A Superadmin-only endpoint creates Officer and Admin accounts. JWT validation compares token roles with current Identity roles, so a role change rejects an old access token. Book reads require Officer or Admin; mutations require Admin. Membership and loan endpoints require Officer. Officer checkout approves and records a loan in one action.

Refresh tokens are stored raw in `IdentityContext`. Successful refresh rotates a valid token; reuse of a revoked token revokes active tokens for that user; logout treats missing, invalid, expired, or revoked supplied tokens as a non-failing path. Cookie paths differ across login, refresh, 2FA, and logout, so inspect the entire flow before changing it.

API keys arrive in `X-API-Key`, are compared as SHA-256 hashes, and are eligible only when key and client status permit. Failures intentionally conceal the reason. Raw values are returned only during creation and must never be persisted or logged.

Configured policies are not necessarily applied. Country permission policies exist but are not attached to Country actions; API-client administration lacks explicit authorization; and audit queries require authentication but lack a dedicated role/permission policy. API-client `RateLimitPerMinute` is stored but not enforced. Initial password setup requires the confirmation token; callback base URLs remain client supplied and need a trust review. These are risks, not patterns to reproduce.

Documentation and logs may name configuration keys but must never include secret values.
