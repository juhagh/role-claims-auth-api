[![CI](https://github.com/juhagh/role-claims-auth-api/actions/workflows/ci.yml/badge.svg)](https://github.com/juhagh/role-claims-auth-api/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql&logoColor=white)](https://www.postgresql.org)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com)
[![JWT](https://img.shields.io/badge/Auth-JWT-black?logo=jsonwebtokens)](https://jwt.io)
[![Identity](https://img.shields.io/badge/ASP.NET-Identity-512BD4?logo=dotnet)](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
[![RBAC](https://img.shields.io/badge/Authorization-RBAC%20%2B%20Claims-green)](https://learn.microsoft.com/aspnet/core/security/authorization/policies)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

# RoleClaimsApp

An ASP.NET Core Web API that demonstrates authentication and authorization patterns commonly used in real backend systems, including JWT access tokens, refresh token rotation, ASP.NET Core Identity, and policy-based access control using roles and claims.

---

## Overview

This project focuses on backend auth concerns that go beyond basic `[Authorize]` usage. It shows how to build a token-based API with clear separation between authentication, authorization, token lifecycle management, and persistence.

Key areas covered include:

- **JWT authentication** with short-lived access tokens
- **Refresh token rotation** with revocation and replacement tracking
- **Secure refresh token storage** using SHA-256 hashing rather than storing raw tokens
- **Policy-based authorization** for centrally managed role-based and claim-based access rules
- **Correct API behavior** with `401 Unauthorized` and `403 Forbidden` handled appropriately for API clients
- **Layered structure** with responsibilities separated across controllers, services, authorization policies, and data access

---

## What This Project Demonstrates

This project was built to practice authentication and authorization patterns that are relevant in real backend applications.

- **Access token + refresh token flow**  
  Users authenticate with username and password, receive a short-lived JWT access token for API calls, and use a refresh token to obtain a new access token when needed.

- **Refresh token rotation**  
  Each refresh request issues a new refresh token and revokes the previous one. This reduces the value of a stolen refresh token and makes token misuse easier to detect.

- **Hashed refresh token storage**  
  Refresh tokens are hashed with SHA-256 before being persisted. The raw token is only returned to the client and is never stored directly in the database.

- **Role-based and claim-based authorization**  
  Access rules are defined centrally as policies and then applied to endpoints. This keeps authorization logic consistent and easier to maintain.

- **API-friendly auth behavior**  
  The application is configured for API consumers, so unauthorized and forbidden requests return proper HTTP status codes rather than redirecting to login pages.

- **Service separation**  
  JWT generation and cryptographic operations are separated from refresh token persistence and lifecycle handling, making the design easier to test and reason about.

---

## Tech Stack

- **Framework:** ASP.NET Core (.NET 9)
- **Authentication:** ASP.NET Core Identity + JWT Bearer
- **Authorization:** Policy-based authorization with roles and claims
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core with Npgsql
- **Security:** `System.Security.Cryptography` (`RandomNumberGenerator`, `SHA256`)

---

## Project Structure

```text
RoleClaimsApp/
├── Authorization/              # Centralized authorization policies
├── Controllers/
│   ├── Admin/                  # Admin-only management endpoints
│   ├── AuthController.cs       # Login, refresh, logout
│   └── ProtectedUsersController.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── IdentitySeeder.cs       # Seeds default admin user, role, and claim
├── Migrations/                 # EF Core migrations
├── Models/
│   ├── ApplicationUser.cs      # Extended Identity user
│   └── RefreshToken.cs         # Refresh token entity with revocation metadata
└── Security/
    ├── TokenService.cs         # JWT creation, token generation, hashing
    └── RefreshTokenService.cs  # Token validation, rotation, revocation
```

---

## Configuration

### Prerequisites
- .NET 9 SDK

- PostgreSQL running locally

### Secrets

The JWT signing key is **not committed to source control.**

For local development, provide it through environment variables or user secrets.

```bash
# Windows (PowerShell)
$env:Jwt__Key="YOUR_DEV_SECRET_KEY"

# Linux / macOS
export Jwt__Key="YOUR_DEV_SECRET_KEY"
```

The value in `appsettings.json` should only be a placeholder that documents the required configuration.

Example development configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=roleclaimsdb;Username=youruser;Password=yourpassword"
  },
  "Jwt": {
    "Key": "your-secret-key-at-least-32-characters-long",
    "Issuer": "RoleClaimsApp",
    "Audience": "RoleClaimsApp",
    "AccessTokenMinutes": 10,
    "RefreshTokenDays": 7
  }
}
```


---

## Running the Application

Apply migrations and start the API:

```bash
dotnet ef database update
dotnet run
```

On startup, the application seeds a default admin user for local testing:

| Username | Password     | Role  | Claims          |
|----------|--------------|-------|-----------------|
| admin    | Password123! | Admin | Department=IT   |

---

## API Endpoints

### Authentication

| Method | Endpoint          | Auth Required | Description                                         |
|--------|-------------------|----------------|-----------------------------------------------------|
| POST   | /api/auth/login   | No             | Authenticate and receive access and refresh tokens  |
| POST   | /api/auth/refresh | No             | Rotate the refresh token and issue a new access token |
| POST   | /api/auth/logout  | No             | Revoke active refresh tokens for the user           |


### Protected Endpoints

| Method | Endpoint                 | Requirement            | Description                     |
|--------|---------------------------|-------------------------|---------------------------------|
| GET    | /api/users/admin         | Role: Admin            | Example of role-based access    |
| GET    | /api/users/it            | Claim: Department=IT   | Example of claim-based access   |
| DELETE | /api/users/claims/department | Role: Admin        | Remove Department claim from admin user |

### Admin User Management

| Method | Endpoint                               | Requirement | Description          |
|--------|------------------------------------------|-------------|----------------------|
| POST   | /api/admin/users/{username}/claims       | Role: Admin | Add a claim to a user |

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

### Example Add Claim Request

```json
{
  "type": "Department",
  "value": "IT"
}
```

---

## Key Design Decisions

### Why hash refresh tokens?

If raw refresh tokens are stored in the database and the database is compromised, those tokens could be used immediately. Hashing them with SHA-256 means the stored value is not directly usable without the original token.

### Why rotate refresh tokens?

Refresh token rotation makes each refresh token single-use. When a token is exchanged, it is revoked and replaced with a new one. This reduces replay risk and gives better visibility into suspicious reuse attempts.

### Why track ReplacedByTokenId?

Tracking replacement relationships allows the token chain to be reconstructed. That can be useful for debugging, auditing, and reasoning about token history for a given session.

### Why separate TokenService and RefreshTokenService?

`TokenService` handles token creation and cryptographic operations. `RefreshTokenService` handles persistence and lifecycle management. Separating these responsibilities keeps the design cleaner and easier to test.

### Why use policies instead of checking roles directly in controllers?

Centralized policies keep authorization rules in one place. That makes the application easier to maintain and change as access requirements evolve.

---

## Testing

This project was tested with API requests covering the main authentication and authorization flows, including:
 - successful login and token issuance
 - refresh token rotation
 - rejection of revoked refresh tokens
 - role-protected endpoint access
 - claim-protected endpoint access
 - logout revoking active refresh tokens

---

## What I Learned

This project helped reinforce several backend development concepts:
 - how ASP.NET Core Identity and JWT Bearer authentication fit together
 - the difference between authentication and authorization
 - how refresh token lifecycle management differs from access token validation
 - how to structure auth-related concerns into smaller, testable services
 - why API authentication behavior should differ from browser-oriented MVC defaults

---

## Limitations and Possible Improvements

This is a portfolio and learning project, not a production-ready identity platform. Some useful next improvements would be:
 - refresh token reuse detection with stronger compromise handling
 - rate limiting and login lockout protections
 - audit logging for security-sensitive actions
 - email confirmation and password reset flows
 - Docker-based local environment setup
 - automated integration tests for the full auth flow

---

## Notes

This project is intended to demonstrate backend security patterns and design decisions in a compact, understandable codebase. The goal was not to build a full identity product, but to implement and explain the core pieces clearly and correctly.
