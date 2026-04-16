[![CI](https://github.com/juhagh/role-claims-auth-api/actions/workflows/ci.yml/badge.svg)](https://github.com/juhagh/role-claims-auth-api/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com)
[![JWT](https://img.shields.io/badge/Auth-JWT-black?logo=jsonwebtokens)](https://jwt.io)
[![Identity](https://img.shields.io/badge/ASP.NET-Identity-512BD4?logo=dotnet)](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
[![RBAC](https://img.shields.io/badge/Authorization-RBAC%20%2B%20Claims-green)](https://learn.microsoft.com/aspnet/core/security/authorization/policies)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

# RoleClaimsApp

An ASP.NET Core Web API demonstrating authentication and authorization patterns commonly used in real backend systems, including JWT access tokens, refresh token rotation, ASP.NET Core Identity, and policy-based access control using roles and claims.

---

## Overview

This project focuses on backend auth concerns that go beyond basic `[Authorize]` usage. It shows how to build a token-based API with clear separation between authentication, authorization, token lifecycle management, and persistence.

Key areas covered include:

- **JWT authentication** with short-lived access tokens and unique `jti` claim per token
- **Refresh token rotation** with revocation and replacement tracking
- **Secure refresh token storage** using SHA-256 hashing rather than storing raw tokens
- **Policy-based authorization** for centrally managed role-based and claim-based access rules
- **Correct API behavior** with `401 Unauthorized` and `403 Forbidden` handled appropriately for API clients
- **Identity-based logout** using JWT claims to revoke all active sessions without requiring the client to send a refresh token
- **Fully containerised** with Docker Compose for one-command local setup
- **Automated integration tests** covering the full auth and token lifecycle

---

## What This Project Demonstrates

- **Access token + refresh token flow**
  Users authenticate with username and password, receive a short-lived JWT access token for API calls, and use a refresh token to obtain a new access token when needed.

- **Refresh token rotation**
  Each refresh request issues a new refresh token and revokes the previous one. This reduces the value of a stolen refresh token and makes token misuse easier to detect.

- **Unique token identity via jti**
  Every access token includes a `jti` (JWT ID) claim — a `Guid` generated at issuance. This guarantees token uniqueness even when issued within the same minute.

- **Hashed refresh token storage**
  Refresh tokens are hashed with SHA-256 before being persisted. The raw token is only returned to the client and never stored directly in the database.

- **Role-based and claim-based authorization**
  Access rules are defined centrally as policies and applied to endpoints. This keeps authorization logic consistent and easier to maintain.

- **API-friendly auth behavior**
  The application is configured for API consumers, so unauthorized and forbidden requests return proper HTTP status codes rather than redirecting to login pages.

- **Service separation**
  JWT generation and cryptographic operations are separated from refresh token persistence and lifecycle handling, making the design easier to test and reason about.

---

## Tech Stack

- **Framework:** ASP.NET Core (.NET 10)
- **Authentication:** ASP.NET Core Identity + JWT Bearer
- **Authorization:** Policy-based authorization with roles and claims
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core with Npgsql
- **Security:** `System.Security.Cryptography` (`RandomNumberGenerator`, `SHA256`)
- **Testing:** xUnit, `WebApplicationFactory`
- **Infrastructure:** Docker, Docker Compose
- **CI:** GitHub Actions

---

## Project Structure

```text
src/
  RoleClaimsApp/
    Authorization/              # Centralized authorization policies
    Controllers/
      Admin/                    # Admin-only management endpoints
      AuthController.cs         # Login, refresh, logout
      ProtectedUsersController.cs
    Data/
      ApplicationDbContext.cs
      IdentitySeeder.cs         # Seeds default users, roles, and claims
    Migrations/                 # EF Core migrations
    Models/
      ApplicationUser.cs        # Extended Identity user
      RefreshToken.cs           # Refresh token entity with revocation metadata
    Security/
      TokenService.cs           # JWT creation, token generation, hashing
      RefreshTokenService.cs    # Token validation, rotation, revocation

tests/
  RoleClaimsApp.Tests/
    AuthControllerTests.cs      # Integration tests for auth and token lifecycle
    RoleClaimsWebApplicationFactory.cs
    Models/
      JwtResponse.cs
```

---

## Running Locally

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Start the full stack

```bash
docker-compose up -d
```

This starts PostgreSQL and the API. Database migrations and user seeding are applied automatically on startup. The API is available at `http://localhost:8080`.

### Running without Docker (development)

```bash
docker-compose up -d postgres-dev
dotnet ef database update --project src/RoleClaimsApp/RoleClaimsApp.csproj
dotnet run --project src/RoleClaimsApp
```

---

## Seeded Users

| Username | Password     | Role  | Claims        |
|----------|--------------|-------|---------------|
| admin    | Password123! | Admin | Department=IT |
| user     | Password123! | —     | —             |

---

## API Endpoints

### Authentication

| Method | Endpoint           | Auth Required | Description                                        |
|--------|--------------------|---------------|----------------------------------------------------|
| POST   | /api/auth/login    | No            | Authenticate and receive access and refresh tokens |
| POST   | /api/auth/refresh  | No            | Rotate the refresh token and issue a new access token |
| POST   | /api/auth/logout   | Yes           | Revoke all active refresh tokens for the current user |

### Protected Endpoints

| Method | Endpoint                     | Requirement          | Description                       |
|--------|------------------------------|----------------------|-----------------------------------|
| GET    | /api/users/admin             | Role: Admin          | Example of role-based access      |
| GET    | /api/users/it                | Claim: Department=IT | Example of claim-based access     |
| DELETE | /api/users/claims/department | Role: Admin          | Remove Department claim from admin user |

### Admin User Management

| Method | Endpoint                         | Requirement | Description           |
|--------|----------------------------------|-------------|-----------------------|
| POST   | /api/admin/users/{username}/claims | Role: Admin | Add a claim to a user |

### Example Login Request

```json
{
  "username": "admin",
  "password": "Password123!"
}
```

### Example Login Response

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque-token>"
}
```

---

## Testing

Integration tests cover the full authentication and authorization lifecycle using `WebApplicationFactory` against a real PostgreSQL test database.

### Test coverage includes

- login with valid credentials returns access and refresh tokens
- login with wrong password returns 401
- login with non-existent user returns 401
- role-protected endpoint grants access to admin user
- role-protected endpoint returns 403 for user without role
- claim-protected endpoint grants access to user with correct claim
- claim-protected endpoint returns 403 for user without claim
- unauthenticated request returns 401
- refresh token rotation returns new unique tokens
- used refresh token returns 401 on reuse
- logout revokes all tokens — subsequent refresh returns 401

### Running tests

```bash
# Start the test database first
docker compose up -d postgres-test

# Run tests
dotnet test
```

---

## Key Design Decisions

### Why hash refresh tokens?

If raw refresh tokens are stored in the database and the database is compromised, those tokens could be used immediately. Hashing them with SHA-256 means the stored value is not directly usable without the original token.

### Why rotate refresh tokens?

Refresh token rotation makes each refresh token single-use. When a token is exchanged, it is revoked and replaced with a new one. This reduces replay risk and gives better visibility into suspicious reuse attempts.

### Why add a jti claim?

The `jti` (JWT ID) is a standard JWT claim that uniquely identifies each token. Without it, two tokens issued to the same user within the same minute could be identical, making it impossible to distinguish them for auditing or revocation purposes.

### Why use identity-based logout?

The logout endpoint uses `[Authorize]` and reads the user identity from the JWT rather than requiring the client to send a refresh token. This is cleaner for API consumers and ensures only authenticated users can trigger a logout.

### Why track ReplacedByTokenId?

Tracking replacement relationships allows the token chain to be reconstructed for debugging, auditing, and reasoning about token history for a given session.

### Why separate TokenService and RefreshTokenService?

`TokenService` handles token creation and cryptographic operations. `RefreshTokenService` handles persistence and lifecycle management. Separating these responsibilities keeps the design cleaner and easier to test.

### Why use policies instead of checking roles directly in controllers?

Centralized policies keep authorization rules in one place. That makes the application easier to maintain and change as access requirements evolve.

---

## Limitations and Possible Improvements

- refresh token reuse detection with stronger compromise handling
- rate limiting and login lockout protections
- audit logging for security-sensitive actions
- email confirmation and password reset flows
- full user management endpoints (list, create, delete users)

---

## Notes

This project is intended to demonstrate backend security patterns and design decisions in a compact, understandable codebase. The goal was not to build a full identity product, but to implement and explain the core pieces clearly and correctly.
