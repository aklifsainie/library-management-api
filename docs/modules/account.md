# Account Module

## Ownership and flow

Account owns registration, email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup. `AccountController` dispatches custom mediator requests; handlers coordinate with `IAccountService`; the service uses ASP.NET Core Identity managers, `IdentityContext`, and SMTP where needed.

## Observed behavior

- `POST /api/account/staff` is Superadmin-only and creates an Officer or Admin without a password. It sends confirmation email before committing Identity creation; delivery itself cannot participate in the database transaction.
- A `Superadmin` can set another user's single role to `Officer`, `Admin`, or `Superadmin` through `PUT /api/account/{userId}/role`. Privileged assignments require an active account with confirmed email and a password. A Superadmin cannot change their own role.
- Valid confirmation sets Identity email confirmation and `EmailVerifiedAt` in UTC. Initial-password setup refuses an account that already has a password.
- Profile update requests mark the account configured. Authenticator setup resets a key without enabling 2FA; successful enable verifies a code, enables 2FA, and creates recovery codes.
- A Superadmin can resend a confirmation email for an active staff account whose invitation was lost.
- Password changes update the Identity security stamp. The current JWT/refresh design does not validate that stamp, so this is not session invalidation.

## Risks and evidence boundaries

`set-password` is public but requires the original email confirmation token, confirmed email, and an active staff role. Disabling 2FA clears custom user fields but does not demonstrably reset all Identity token-store material. Repeated enable operations can issue fresh recovery codes. Callback base URLs are client supplied and form a security trust boundary.

These are source-derived observations, not owner-confirmed product requirements. See the local `AGENTS.md` and [security guidance](../architecture/authentication-authorization.md).

## Role provisioning

The API creates the three Identity roles (`Superadmin`, `Officer`, `Admin`) on startup. It does not create an account or change an existing user's role during normal startup. Existing users retain their current roles, including no role.

To provision the first Superadmin, an operator runs the command with the intended connection string and a trusted frontend callback base URL:

```powershell
dotnet run --project .\library.management.api\library.management.api.csproj -- --bootstrap-superadmin admin@example.com https://library.example.com
```

This one-time command refuses to run if a Superadmin already exists. It creates an account and sends confirmation email when the address is new, or promotes an existing fully configured account, then exits without starting the API. Run it only for the intended database; it writes Identity data. The invited user confirms email and supplies the same confirmation token to `set-password`. Subsequent changes use the Superadmin-only role endpoint with `{"role":"Admin"}` or `{"role":"Officer"}`.
