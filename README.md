# Work Items API

[![CI](https://github.com/AdrienAmoroso/workitems-api/actions/workflows/ci.yml/badge.svg)](https://github.com/AdrienAmoroso/workitems-api/actions/workflows/ci.yml)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Angular](https://img.shields.io/badge/Angular-19-DD0031?logo=angular)
![License](https://img.shields.io/badge/License-MIT-green)

A full-stack **Work Items management system** (similar to Jira tickets) built with ASP.NET Core and Angular. This project demonstrates enterprise-level development practices including JWT authentication, clean architecture, and comprehensive testing.

## What This Project Demonstrates

**Backend**
- ASP.NET Core Web API with Controllers and REST conventions
- Entity Framework Core with SQLite and Code-First Migrations
- JWT Authentication with BCrypt password hashing
- Clean Architecture (Controllers → Services → Data Layer)
- Input Validation, Pagination, Filtering & Sorting
- Unit & Integration Tests with xUnit
- CI/CD Pipeline with GitHub Actions

**Frontend**
- Angular 19 with standalone components
- Angular Material UI
- Reactive Forms with validation
- JWT token management with HTTP interceptor
- Route guards for protected pages

## Tech Stack

| Backend | Frontend |
|---------|----------|
| ASP.NET Core 10 | Angular 19 |
| C# 13 | TypeScript |
| Entity Framework Core | Angular Material |
| SQLite | RxJS |
| JWT Bearer Auth | SCSS |
| xUnit + Moq | |

## Project Structure

```
workitems-api/
├── src/WorkItems.Api/          # ASP.NET Core Web API
│   ├── Contracts/              # Request/Response DTOs
│   ├── Data/                   # DbContext
│   ├── Domain/                 # Entity models
│   ├── Endpoints/              # Controllers
│   ├── Migrations/             # EF Core migrations
│   └── Services/               # Business logic
├── tests/WorkItems.Api.Tests/  # Unit & Integration tests
└── frontend/workitems-web/     # Angular application
    └── src/app/
        ├── core/               # Services, guards, interceptors
        ├── features/           # Auth & Work Items pages
        └── shared/             # Navbar, dialogs
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/AdrienAmoroso/workitems-api.git
cd workitems-api

# Terminal 1: Start Backend (migrations apply automatically)
dotnet run --project src/WorkItems.Api

# Terminal 2: Start Frontend
cd frontend/workitems-web
npm install
ng serve --open
```

| Service | URL |
|---------|-----|
| Angular App | http://localhost:4200 |
| API (Swagger) | http://localhost:5000 |

### Run Tests

```bash
dotnet test
```

## API Endpoints

### Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register a new user |
| POST | `/api/auth/login` | Login and get JWT token |

### Work Items

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/work-items` | No | List all (paginated) |
| GET | `/api/work-items/{id}` | No | Get by ID |
| POST | `/api/work-items` | Yes | Create |
| PUT | `/api/work-items/{id}` | Yes | Update |
| DELETE | `/api/work-items/{id}` | Yes | Delete |

**Query Parameters:** `page`, `pageSize`, `status`, `priority`, `sortBy`, `sortDir`

## Frontend Pages

| Page | Route | Description |
|------|-------|-------------|
| Login | `/auth/login` | User authentication |
| Register | `/auth/register` | New user registration |
| Work Items | `/work-items` | List with filtering & sorting |
| Detail | `/work-items/:id` | View single item |
| Create/Edit | `/work-items/new` | Form for CRUD operations |

## Test Coverage

**34 tests** covering:
- WorkItemService CRUD operations
- Authentication flow (register, login)
- Protected endpoints with JWT validation
- Error handling (404, 401, 400)

## Roadmap

- [x] ASP.NET Core Web API
- [x] JWT Authentication
- [x] Angular Frontend with Material UI
- [x] GitHub Actions CI
- [ ] Role-based authorization
- [ ] Docker support
- [ ] Health checks endpoint

## Author

**Adrien Amoroso** - [GitHub](https://github.com/AdrienAmoroso) | [LinkedIn](https://www.linkedin.com/in/adrien-amoroso/)

## License

MIT License - see [LICENSE](LICENSE) for details.
