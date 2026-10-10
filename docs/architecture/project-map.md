# Project Map

| Project | Type | Responsibility | References | Persistence/API/tests |
|---|---|---|---|---|
| `library.management.api` | ASP.NET Core Web SDK | Composition root, controllers, JWT/API-key handlers, policies, rate limiting, Swagger | Application, Identity, Infrastructure | Executable API |
| `library.management.application` | Class library | Commands, queries, handlers, contracts, custom mediator, results, application services | Domain, Identity | Application logic |
| `library.management.domain` | Class library | Shared entities, ViewModels, enums, base models, unit-of-work contract | None | Domain models |
| `library.management.infrastructure` | Class library | Application EF context/configurations/migrations, repositories, API-key and audit implementations | Application, Domain | SQL Server persistence |
| `library.management.identity` | Class library | Identity entities, account/authentication services, Identity context and migrations | Domain | Identity persistence |
| `library.management.test` | Test project | Handler-level unit tests | Application, Domain | xUnit tests |

## Package observations

- Every project targets `net8.0` with nullable reference types enabled.
- API and Infrastructure reference EF Core 9.0.7; Identity references EF Core 8.0.22.
- Application references FluentResults 4.0.0; Identity references FluentResults 3.11.0.
- No central package management, `global.json`, `Directory.Build.*`, Docker files, worker projects, or CI workflow were found at bootstrap time.

These version differences are existing constraints, not permission to normalize packages during feature work.
