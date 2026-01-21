You are my coding assistant for a portfolio project. I want to build a small, clean, production-style .NET project that proves I can handle common ESN/enterprise expectations even as a junior.

Goal
- Create a public GitHub repo that showcases:
  1) ASP.NET Core Web API
  2) Entity Framework Core + migrations
  3) Simple JWT authentication (or at least the full auth structure)
  4) Tests: xUnit + a few integration tests
  5) CI with GitHub Actions: build + test on push/PR

Context (my profile)
- Junior software engineer, dev-oriented with strong QA mindset
- Experience: Technical QA internship in a game studio (Rebound), built internal tools in C#, regression testing, bug analysis, explored test automation & localization automation
- Target: First job in Bordeaux as C#/.NET junior in an ESN
- I want this project to be a “portfolio-ready” proof of skills, with good structure, documentation, and realistic practices.

Project naming
- Repository name: dotnet-portfolio-api
- Solution: DotNetPortfolioApi
- Projects:
  - src/DotNetPortfolioApi.Api (Web API)
  - tests/DotNetPortfolioApi.Api.Tests (xUnit + integration tests)

Constraints & preferences
- Keep it simple but professional: clean architecture-lite (Controller/Minimal API + Services + Data layer), DI, configuration per environment.
- Use SQLite for local dev (simple setup), with EF Core migrations.
- Add Swagger/OpenAPI.
- Add basic input validation (DataAnnotations or FluentValidation; pick one).
- Avoid overengineering (no microservices, no DDD rabbit hole). But the code should look like real work.

Suggested domain (you choose one and stick to it)
Option A: “Work Items” API (tickets/tasks)
- Entities: WorkItem { Id, Title, Description, Status, Priority, CreatedAt, UpdatedAt }
- Endpoints: CRUD + filtering (status/priority) + pagination + sorting.
Option B: “Books Catalog” API
- Entities: Book { Id, Title, Author, Isbn, PublishedAt } and maybe Category
- Endpoints: CRUD + search + pagination.
Pick the domain that best demonstrates realistic API patterns.

Auth requirements (JWT)
- Implement JWT-based auth with:
  - POST /auth/register
  - POST /auth/login
  - Protected endpoints for create/update/delete
- Use a simple User entity + password hashing (ASP.NET Core Identity is allowed, but it might be heavy; if you use it, keep it minimal).
- At minimum: show correct structure (auth service, token generation, config, middleware).

Testing requirements
- Unit tests for at least one service (happy path + error case).
- Integration tests using WebApplicationFactory:
  - One test for a public endpoint (GET list)
  - One test for a protected endpoint (POST create) including obtaining a JWT and calling the endpoint with Authorization header
- Tests should run in CI.
- For integration tests, use an in-memory DB or SQLite in-memory (your choice), but keep it reliable.

CI requirements (GitHub Actions)
- On push and pull request:
  - restore
  - build (Release)
  - test
- Provide a badge in README.

Documentation
- Write a strong README:
  - What the project demonstrates (bullet list)
  - Tech stack
  - How to run locally
  - How to run migrations
  - How to run tests
  - Example curl commands
- Add a simple roadmap section (optional).

What I need from you (Claude Haiku)
- Guide me step by step after initialization:
  1) Add EF Core + SQLite + migrations
  2) Build the chosen domain: entities, DbContext, endpoints, services
  3) Add validation + error handling (ProblemDetails)
  4) Add JWT auth (register/login + protected endpoints)
  5) Add unit tests + integration tests
  6) Add GitHub Actions CI
  7) Polish: README + small refactors + consistent naming

Important
- Provide commands and code snippets that I can copy/paste.
- Keep explanations short and practical.
- Favor a clean structure and naming conventions typical in .NET projects.
