# SkillHub

SkillHub manages in-person workshops and participant enrollments. It prevents overbooking, duplicate active registrations and overlapping participant schedules. Employees publish workshops, import registrations from Excel, cancel enrollments and review attendance demand. Participants register, browse, enroll, cancel and download a PDF enrollment certificate.

## Technologies

- C# and .NET 10 / ASP.NET Core controllers
- Clean Architecture, native dependency injection, Repository pattern and DTOs
- ASP.NET Core Identity with Participant and Employee roles
- JWT bearer authentication and Swagger UI
- Entity Framework Core 10 with Code First migrations and Npgsql
- PostgreSQL 17 in Docker Compose
- ClosedXML for Excel and PDFsharp 6.2.4 for PDF generation
- HTML, CSS and JavaScript participant client, served by the API and consuming its JWT endpoints
- xUnit domain tests and Python end-to-end tests against real PostgreSQL

## Run locally

Requirements: .NET 10 SDK, Docker with Docker Compose, Bash and Python 3. Linux is supported; use WSL on Windows for the helper scripts. No separate frontend installation is necessary.

From the repository root:

```bash
./scripts/run-local.sh
```

The script creates a local `.env` with random secrets when needed, starts PostgreSQL, restores/builds the API and starts it. On startup, the API applies the included EF Core migration and idempotently seeds the supplied workbook.

- Participant application: **http://localhost:5080**
- Swagger UI: **http://localhost:5080/swagger**
- OpenAPI document: **http://localhost:5080/swagger/v1/swagger.json**
- Database: **localhost:5432**, database **skillhub**, user **skillhub**

Database passwords and the JWT signing secret are stored only in the ignored `.env`. Never commit this file. PostgreSQL binds only to the host loopback interface and persists data in a Docker volume. Change `POSTGRES_PORT` in `.env` if port 5432 is already occupied. API configuration is passed as `ConnectionStrings__DefaultConnection` and `Jwt__Key` environment variables; no database credentials or signing key are hardcoded in application settings.

To run the components separately:

```bash
source scripts/load-env.sh
docker compose up -d --wait
dotnet run --project src/SkillHub.Api --no-launch-profile --urls http://localhost:5080
```

Stop the API with Ctrl+C. Stop PostgreSQL while preserving its data:

```bash
docker compose down
```

The local configuration uses HTTP on loopback. For a deployment, supply private secrets, configure HTTPS, set `AllowedHosts`, and replace the assessment accounts/passwords. Swagger is enabled only in Development.

## Assessment accounts and seed data

Employees can log in immediately after the first startup. An example instructor/employee account is:

| Role        | Email                       | Password            |
| ----------- | --------------------------- | ------------------- |
| Employee    | `andres.mejia@correo.com` | `Instructor2026*` |
| Participant | `jorge.perez@correo.com`  | `Skillhub2026*`   |

The original `data/SkillHub_Data.xlsx` seeds **6 instructors, 30 participants and 15 workshops** from the Instructors, Participants and Workshops sheets. All seeded instructors receive Employee access. Registration always creates a Participant, regardless of any extra role fields sent by a client. Startup does not reset existing passwords or duplicate records.

**The Enrollments sheet is never imported at startup.** Upload the original workbook through the import endpoint to process those pending rows. The supplied workbook contains assessment credentials; treat it as test data.

Excel workshop dates are interpreted in **America/Bogota (UTC−05:00)**. The database and JSON responses store instants in UTC. Historical seed workshops retain their original dates; the public catalog only includes future workshops. To demonstrate the application after all supplied dates have passed, create a future workshop using an employee account in Swagger.

## API usage

1. Open Swagger and call `POST /api/v1/auth/login` using an assessment account.
2. Copy `accessToken` from the response.
3. Click **Authorize** and paste the token. Swagger adds the `Bearer` prefix automatically.
4. Use employee endpoints from Swagger. Use the participant application or a participant token for enrollment operations.

| Method | Route                                    | Access                                         |
| ------ | ---------------------------------------- | ---------------------------------------------- |
| POST   | `/api/v1/auth/register`                | Public; creates a participant                  |
| POST   | `/api/v1/auth/login`                   | Public                                         |
| GET    | `/api/v1/workshops`                    | Public; future workshops with calculated seats |
| GET    | `/api/v1/workshops/{id}`               | Public                                         |
| POST   | `/api/v1/workshops`                    | Employee                                       |
| GET    | `/api/v1/workshops/{id}/enrollments`   | Employee                                       |
| GET    | `/api/v1/instructors`                  | Employee                                       |
| POST   | `/api/v1/enrollments`                  | Participant                                    |
| GET    | `/api/v1/enrollments/mine`             | Participant; own records only                  |
| PATCH  | `/api/v1/enrollments/{id}/cancel`      | Owner or employee                              |
| POST   | `/api/v1/enrollments/import`           | Employee; multipart form field `File`        |
| GET    | `/api/v1/enrollments/{id}/certificate` | Owner or employee; active enrollment only      |
| GET    | `/api/v1/reports/top-workshops`        | Employee                                       |

Example login body:

```json
{
  "email": "andres.mejia@correo.com",
  "password": "Instructor2026*"
}
```

Example workshop body (choose future dates when running):

```json
{
  "code": "FOT-100",
  "title": "Photography fundamentals",
  "description": "Practice composition and natural light.",
  "instructorEmail": "andres.mejia@correo.com",
  "startTime": "2030-12-05T08:00:00-05:00",
  "endTime": "2030-12-05T12:00:00-05:00",
  "capacity": 15
}
```

Enrollment body: `{ "workshopId": "<workshop UUID>" }`. The participant identity always comes from the JWT, not the request body.

API errors use ProblemDetails-style responses with a clear message. Invalid request data returns 400, invalid/missing authentication 401, denied access 403, missing resources 404, and conflicting business rules or unique values 409. Tokens expire after 60 minutes by default. Identity temporarily locks an account for 5 minutes after 5 failed password attempts.

### Enrollment rules

- Only active enrollments consume seats and block duplicate or overlapping registrations.
- Workshops must start in the future; the end must follow the start and capacity must be positive.
- Intervals are half open: a workshop can start exactly when another finishes.
- A participant can cancel **at least 24 hours before** the workshop starts, including exactly 24 hours.
- Employees can cancel any enrollment before the workshop starts, including within the 24-hour window.
- Cancellations record the authenticated actor and timestamp and release the seat immediately.
- Reenrollment creates a new active record and preserves the cancelled record for auditing.
- PostgreSQL transactions take deterministic participant and workshop advisory locks in a fixed order. Requests from multiple API processes cannot overbook, duplicate an active registration or bypass a participant's schedule conflicts. A partial unique database index also protects active duplicates.

### Excel import

Upload an `.xlsx` workbook using the `File` multipart field. For the supplied workbook the importer selects **Enrollments**. A single-sheet workbook is also supported. Row 1 must contain `ParticipantEmail` and `WorkshopCode`; header order is flexible. Entirely blank rows are skipped. Formula cells are rejected per row without evaluation; headers must be plain text.

The limit is 5 MB compressed, 30 MB expanded and 5,000 data rows. Invalid workbooks or missing headers return 400. Each nonblank row follows the normal enrollment rules in an independent transaction. A failed row does not undo accepted rows. Rejection row numbers correspond to the original Excel rows, including the header.

```json
{
  "totalRows": 3,
  "enrolledRows": 2,
  "rejectedRows": 1,
  "rejections": [
    { "rowNumber": 4, "reason": "Participant not found." }
  ]
}
```

### PDF certificates

The infrastructure adapter generates a PDF containing SkillHub, “Constancia de inscripción”, participant name/email, workshop code/title, instructor, start/end times, enrollment date and document generation date. Workshop times are rendered in America/Bogota. DejaVu fonts are bundled for portable Unicode support; their license is included under Infrastructure/Fonts.

## Coding conventions

The C# style follows [ApiEmpleados](https://github.com/danielalva0004/ApiEmpleados): one DTO class per file, public properties with validation attributes, explicit constructors with private readonly dependencies, object initializers and conventional method bodies. Class, method and property names remain in English. The `.editorconfig` preserves braces and expanded methods.

For example, registration uses `DTOs/Auth/RegisterUserDto.cs`, login uses `DTOs/Auth/LoginDto.cs`, and the authentication response uses `DTOs/Auth/AuthResponseDto.cs`. There are no grouped positional DTO records.

## Solution structure

```text
src/
  SkillHub.Domain/
    Entities/                 Instructor, Workshop, Enrollment
    Enums/                    EnrollmentStatus
    Exceptions/               BusinessRuleException
    Rules/                    EnrollmentPolicy
    DependencyInjection.cs
  SkillHub.Application/
    DTOs/                     Auth, workshops, enrollments and report contracts
    Enums/                    UserRole
    Exceptions/               Application-level errors
    Interfaces/
      Services/               Application services and infrastructure abstractions
      Repositories/           Persistence contracts and transaction boundary
    Service/                  Authentication, workshop, enrollment and report use cases
    DependencyInjection.cs
  SkillHub.Infrastructure/
    Identity/                 ApplicationUser
    Service/                  JWT, current actor, Excel workbook and PDF services
    Persistence/
      Repository/             EF Core repositories and PostgreSQL transactions
        External/             Adapters that use the Excel and PDF services
      Migrations/             Code First migration and model snapshot
      SkillHubDbContext.cs
      DatabaseInitializer.cs
    Fonts/                    Bundled fonts and license
    DependencyInjection.cs
  SkillHub.Api/
    Controllers/              HTTP endpoints only
    Middleware/               Central exception handling
    Swagger/                  Public endpoint metadata
    wwwroot/                  Participant frontend
    DependencyInjection.cs
    Program.cs
```

Domain has no dependency on Application, Infrastructure, Identity or EF Core. Application depends on Domain and defines its infrastructure abstractions. Infrastructure implements those abstractions. API composes the solution through dependency injection. There is no external HTTP API required by the assessment; the External folder contains the actual Excel/PDF adapters rather than placeholder integrations.

## Database migrations and SQL delivery

The initial migration is included under `src/SkillHub.Infrastructure/Persistence/Migrations`. Restore the local EF tool to manage migrations:

```bash
dotnet tool restore
source scripts/load-env.sh
dotnet ef database update --project src/SkillHub.Infrastructure --startup-project src/SkillHub.Api
```

To add a migration after an intentional model change:

```bash
dotnet ef migrations add MigrationName --project src/SkillHub.Infrastructure --startup-project src/SkillHub.Api --output-dir Persistence/Migrations
```

The API applies migrations and seeds records on startup by default. Set `Database__InitializeOnStartup=false` when an external deployment process manages them.

`docs/migrations.sql` is an idempotent EF Core schema script: it applies only migrations missing from the target database. It does not include seed data; the normal API startup loads the supplied workbook. Regenerate the script after changing migrations:

```bash
dotnet ef migrations script --idempotent --project src/SkillHub.Infrastructure --startup-project src/SkillHub.Api --output docs/migrations.sql
```

`docs/database.sql` contains a PostgreSQL structure/data export of the initialized assessment database. Regenerate it from the running container:

```bash
./scripts/export-database.sh
```

To restore the export into a separate empty database, create that database first and run `psql -U <user> -d <empty_database> -f docs/database.sql`. Do not restore over an already initialized database.

## Verification

Build and run domain tests:

```bash
dotnet build SkillHub.sln -c Release
dotnet test tests/SkillHub.Tests -c Release
```

Run the end-to-end suite (requires Docker, Python 3, curl and `pdftotext`, provided by `poppler-utils` on Ubuntu):

```bash
dotnet build SkillHub.sln
./scripts/test-integration.sh
```

The integration script creates its own temporary PostgreSQL database and an API on port 5081, runs checks, and removes only that temporary database on exit. The main `skillhub` database is not modified. Coverage includes role escalation attempts, ownership, JWT tampering, workshop validation, cancellation deadlines, reenrollment, overlapping/adjacent schedules, concurrent enrollment requests, Excel partial success and malformed inputs, PDF text and long field wrapping, reports, Swagger and participant assets.

Optional real-browser checks also cover participant registration, login, enrollment, PDF download, cancellation, mobile layout and overnight workshop dates:

```bash
npm --prefix tests/browser install
npx --prefix tests/browser playwright install chromium
RUN_BROWSER_TESTS=1 ./scripts/test-integration.sh
```

## Delivery checklist

- Source solution, migration, original workbook and participant client are included.
- Database structure and data export: `docs/database.sql`.
- Author information is included below.
- Publish the project to your own public GitHub repository, retain development commits and include the repository URL with the submission. Keep `.env`, build outputs and IDE metadata excluded using the included `.gitignore`.
- A source archive is included at `artifacts/SkillHub-source.zip`. Regenerate it with `python3 scripts/package-source.py`; it excludes `.env`, build output and IDE metadata. Adjust the delivery format if your Team Leader specifies a different one.

## Author

- Full name: **Daniel Alvarez Diaz**
- Clan: **esthercita**
- Email: **danieleliazalvarezdiaz@gmail.com**
- Identity document: **1043666155**
