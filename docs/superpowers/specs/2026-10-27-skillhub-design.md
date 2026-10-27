# SkillHub design

Implement the supplied SkillHub assessment with .NET 10, ASP.NET Core Identity, JWT, Swagger and PostgreSQL 17 in Docker. The user selected PostgreSQL and asked to continue implementation in the existing workspace.

## Architecture
Domain owns workshop and enrollment invariants. Application owns DTOs, services, role enums, application exceptions and repository/service abstractions. Infrastructure owns Identity, JWT, Excel/PDF adapters under Service and Repository/External, and EF Core under Persistence (DbContext, Repository, Migrations and seed). API contains thin controllers and error/authentication plumbing. Each project has DependencyInjection.cs. The participant web client is served by the API and consumes JWT endpoints.

## Decisions
PostgreSQL is the selected database rather than SQL Server or SQLite. Use database transactions with participant and workshop advisory locks to serialize competing enrollments and cancellations. Adjacent intervals do not overlap. The 24-hour cancellation deadline is inclusive. All instants use UTC; Excel dates are interpreted as America/Bogota local time. Import rows commit independently and retain physical Excel row numbers. Identity registration always assigns Participant. Seeded instructors have Employee access. No third-party API is required, so no invented HTTP integration is added.

## Deliverables
All specified endpoints; English code, comments and README; migration; original seed workbook; participant client; meaningful automated domain and PostgreSQL/API integration checks; SQL export; local run scripts. Preserve canceled enrollment history and cancellation actor/time. Certificate ownership and active status are checked before PDF generation. Seed only Instructors, Participants and Workshops, never Enrollments. Do not invent author identity or GitHub publication; document missing user information.
