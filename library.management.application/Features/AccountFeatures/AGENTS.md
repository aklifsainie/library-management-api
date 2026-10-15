# Account Feature Instructions

Account owns Superadmin-created staff accounts and post-registration configuration: email confirmation, initial and changed passwords, profile, current-account view, and authenticator setup. Members are application-owned records and do not receive Identity accounts.

Controllers dispatch through the custom mediator; handlers delegate Identity-specific multi-step behavior to `IAccountService`. Keep `UserManager`, `SignInManager`, confirmation/password/2FA operations, Identity persistence, and SMTP coordination in that service boundary. Do not add an application repository merely to mirror Country.

Before changing this module, read the closest command/query, `AccountController`, `IAccountService`, `AccountService`, `ApplicationUser`, related Identity configuration, and [the Account module guide](../../../docs/modules/account.md). Inspect Authentication when a password, email, token, or 2FA flow is involved.

Important observed risks: initial password setup is public but requires the original confirmation token; disabling 2FA does not demonstrably clear all Identity token-store material; updating the security stamp does not itself invalidate this application's JWTs or refresh tokens. Treat any correction as a security-sensitive blueprinted change.

Role claim and handler unit tests exist, but Identity-manager behavior still needs integration coverage for confidence beyond repository mocks.
