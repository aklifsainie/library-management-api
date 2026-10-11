# Request Lifecycle

## Country

```text
CountryController -> custom IMediator -> command/query handler
-> ICountryRepository -> CountryRepository / GenericRepository
-> ApplicationDbContext -> SQL Server
```

Mutations save through `IUnitOfWork`; reads use no tracking, include active-state filtering in handlers, and attempt explicit view audits. The EF interceptor writes mutation audits with the application transaction.

## Book

```text
BooksController -> custom IMediator -> Book command/query handler
-> IBookRepository -> BookRepository / GenericRepository
-> ApplicationDbContext -> SQL Server
```

Reads filter active and non-deleted books and attempt explicit view audits. Update and soft-delete use tracked entities; mutations save through `IUnitOfWork` and are audited by the EF interceptor.

## Account and Authentication

```text
AccountController or AuthenticationController -> custom mediator -> handler
-> IAccountService or IAuthenticationService
-> UserManager / SignInManager / IdentityContext / SMTP as applicable
```

Identity services own multi-step confirmation, password, 2FA, token, and refresh workflows. Inspect all related actions when a change affects cookies, current-user access, lockout, active/deleted state, or JWT validation.

## API client and audit

```text
ApiClientController -> IApiClientService -> ApplicationDbContext

AuditLogController -> custom mediator -> GetAuditLogQueryHandler
-> IAuditLogRepository.GetQueryable() -> EF query in Application
```

The first is a direct-service administrative variation. The second couples Application to EF query execution. Preserve them as explicit exceptions rather than copying either flow by default.

## Loan flow

`POST api/v1/loans` authenticates a Member, derives `UserId` from the bearer token, checks Identity account status, loads an active Book, and inserts an open `BookLoan`. A filtered unique SQL index rejects racing borrowers. `GET` requests filter member history by `UserId` and page in the repository. `POST api/v1/loans/{id}/return` checks ownership for Members and records the return time; Admins may return any loan. Reads explicitly write View audit entries, while writes use the EF audit interceptor. See [loan module](../modules/loan.md).
