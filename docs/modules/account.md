# Account Module

## Ownership and flow

Account owns registration, email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup. `AccountController` dispatches custom mediator requests; handlers coordinate with `IAccountService`; the service uses ASP.NET Core Identity managers, `IdentityContext`, and SMTP where needed.

## Observed behavior

- Registration creates an active, non-deleted user without a password, assigns the `Member` role, and attempts confirmation email delivery; email delivery is not transactional with user creation.
- A `Superadmin` can set another user's single role to `Member`, `Admin`, or `Superadmin` through `PUT /api/account/{userId}/role`. Privileged assignments require an active account with confirmed email and a password. A Superadmin cannot change their own role.
- Valid confirmation sets Identity email confirmation and `EmailVerifiedAt` in UTC. Initial-password setup refuses an account that already has a password.
- Profile update requests mark the account configured. Authenticator setup resets a key without enabling 2FA; successful enable verifies a code, enables 2FA, and creates recovery codes.
- Password changes update the Identity security stamp. The current JWT/refresh design does not validate that stamp, so this is not session invalidation.

## Risks and evidence boundaries

`set-password` is public and receives a user ID without observed caller binding. Disabling 2FA clears custom user fields but does not demonstrably reset all Identity token-store material. Repeated enable operations can issue fresh recovery codes. Callback base URLs are client supplied and form a security trust boundary.

These are source-derived observations, not owner-confirmed product requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).

## Role provisioning

The API creates the three Identity roles (`Superadmin`, `Member`, `Admin`) on startup. It does not create an account or change an existing user's role during normal startup. Existing users retain their current roles, including no role.

To provision the first Superadmin, an operator with access to the configured Identity database first registers an account, confirms its email, and configures its password. Then, with the intended connection string configured, run:

```powershell
dotnet run --project .\library.management.api\library.management.api.csproj -- --bootstrap-superadmin admin@example.com
```

This one-time command refuses to run if a Superadmin already exists. It replaces the account's prior role and exits without starting the API. Run it only for the intended database; it writes role records. Do not promote an account before its password is set, because the existing public initial-password endpoint accepts a user ID. Subsequent changes use the Superadmin-only endpoint with a body such as `{ "role": "Admin" }` or `{ "role": "Member" }` to remove administrative access.
