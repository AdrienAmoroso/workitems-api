# DotNet Portfolio API

[![CI](https://github.com/AdrienAmoroso/dotnet-portfolio-api/actions/workflows/ci.yml/badge.svg)](https://github.com/AdrienAmoroso/dotnet-portfolio-api/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/License-MIT-green)

A production-ready ASP.NET Core Web API demonstrating enterprise-level development practices. This project showcases a **Work Items management system** (similar to Jira tickets) with full CRUD operations, JWT authentication, and comprehensive testing.

## What This Project Demonstrates

- **ASP.NET Core Web API** with Controllers and proper REST conventions
- **Entity Framework Core** with SQLite and Code-First Migrations
- **JWT Authentication** with secure password hashing (BCrypt)
- **Clean Architecture** (Controllers → Services → Data Layer)
- **Dependency Injection** throughout the application
- **Input Validation** using Data Annotations
- **Pagination, Filtering & Sorting** on list endpoints
- **Unit Tests** with xUnit and in-memory database
- **Integration Tests** using `WebApplicationFactory`
- **CI/CD Pipeline** with GitHub Actions
- **Swagger/OpenAPI** documentation

## Tech Stack

| Category | Technology |
|----------|------------|
| Framework | ASP.NET Core 10 |
| Language | C# 13 |
| Database | SQLite (EF Core) |
| Authentication | JWT Bearer Tokens |
| Password Hashing | BCrypt |
| API Documentation | Swagger / OpenAPI |
| Testing | xUnit, Moq, WebApplicationFactory |
| CI/CD | GitHub Actions |

## Project Structure

```
dotnet-portfolio-api/
├── .github/
│   └── workflows/
│       └── ci.yml              # GitHub Actions CI pipeline
├── src/
│   └── DotnetPortfolioApi.Api/
│       ├── Contracts/          # DTOs (Request/Response models)
│       ├── Data/               # DbContext & configurations
│       ├── Domain/             # Entity models
│       ├── Endpoints/          # API Controllers
│       ├── Migrations/         # EF Core migrations
│       ├── Services/           # Business logic layer
│       └── Program.cs          # Application entry point
└── tests/
    └── DotnetPortfolioApi.Api.Tests/
        ├── Integration/        # Integration tests
        └── Unit/               # Unit tests
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A code editor (VS Code, Visual Studio, Rider)

### Run Locally

```bash
# Clone the repository
git clone https://github.com/AdrienAmoroso/dotnet-portfolio-api.git
cd dotnet-portfolio-api

# Restore dependencies
dotnet restore

# Run the API (migrations are applied automatically in Development)
dotnet run --project src/DotnetPortfolioApi.Api
```

The API will be available at:
- **Swagger UI**: https://localhost:5001 (or http://localhost:5000)
- **API Base URL**: https://localhost:5001/api

### Run Migrations Manually

```bash
# Apply migrations
dotnet ef database update --project src/DotnetPortfolioApi.Api

# Create a new migration
dotnet ef migrations add MigrationName --project src/DotnetPortfolioApi.Api
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run with coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

## API Documentation

### Authentication Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/register` | Register a new user | No |
| POST | `/api/auth/login` | Login and get JWT token | No |

### Work Items Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/work-items` | Get all work items (paginated) | No |
| GET | `/api/work-items/{id}` | Get a work item by ID | No |
| POST | `/api/work-items` | Create a new work item | Yes |
| PUT | `/api/work-items/{id}` | Update a work item | Yes |
| DELETE | `/api/work-items/{id}` | Delete a work item | Yes |

### Query Parameters (GET /api/work-items)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | int | 1 | Page number |
| `pageSize` | int | 10 | Items per page (max: 100) |
| `status` | enum | - | Filter by status: `Todo`, `InProgress`, `Done` |
| `priority` | enum | - | Filter by priority: `Low`, `Medium`, `High` |
| `sortBy` | string | `createdAt` | Sort field: `createdAt`, `updatedAt`, `title`, `status`, `priority` |
| `sortDir` | string | `desc` | Sort direction: `asc`, `desc` |

## Example Usage (cURL)

### Register a new user

```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "johndoe",
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }'
```

### Login

```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "johndoe",
    "password": "SecurePassword123!"
  }'
```

### Get all work items (with filtering)

```bash
# Get all work items
curl https://localhost:5001/api/work-items

# Get high priority TODO items, page 1
curl "https://localhost:5001/api/work-items?status=Todo&priority=High&page=1&pageSize=5"
```

### Create a work item (requires authentication)

```bash
# Replace YOUR_TOKEN with the token from login response
curl -X POST https://localhost:5001/api/work-items \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "title": "Implement new feature",
    "description": "Add export functionality to the dashboard",
    "priority": "High"
  }'
```

### Update a work item

```bash
curl -X PUT https://localhost:5001/api/work-items/{id} \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "title": "Implement new feature",
    "description": "Add export functionality to the dashboard",
    "status": "InProgress",
    "priority": "High"
  }'
```

### Delete a work item

```bash
curl -X DELETE https://localhost:5001/api/work-items/{id} \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Test Coverage

The project includes **34 tests** covering:

### Unit Tests
- WorkItemService CRUD operations
- Pagination logic
- Filtering and sorting
- Error handling (NotFoundException)

### Integration Tests
- Authentication flow (register, login, validation)
- Work items CRUD with real HTTP requests
- Protected endpoints with JWT validation
- Error responses (404, 401, 400)

## Roadmap

- [ ] Add role-based authorization (Admin, User)
- [ ] Add work item comments/history
- [ ] Add Docker support
- [ ] Add rate limiting
- [ ] Add response caching
- [ ] Add health checks endpoint

## Author

**Adrien Amoroso**

- GitHub: [@AdrienAmoroso](https://github.com/AdrienAmoroso)
- LinkedIn: [Your LinkedIn](https://linkedin.com/in/your-profile)

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

If you found this project helpful, please consider giving it a star!
