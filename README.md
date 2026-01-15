# Role & Claims Authentication API

This project is an **ASP.NET Core Web API** demonstrating **production-style authentication and authorization** using:

- ASP.NET Identity (PostgreSQL-backed)
- JWT access tokens
- Rotating, database-backed refresh tokens
- Role-based and claim-based authorization
- Secure logout and token revocation

The focus of this project is **correct architecture and security semantics**, not UI or framework demos.

---

## Key Features

- **Identity-backed authentication**
  - Users, roles, and claims stored in PostgreSQL
  - Secure password hashing via ASP.NET Identity

- **JWT-based authorization**
  - Short-lived access tokens
  - Stateless request authentication

- **Refresh token implementation (production-style)**
  - Opaque refresh tokens
  - Tokens stored hashed in the database
  - Rotation on every refresh
  - Revocation and logout support

- **Authorization examples**
  - Role-based authorization (Admin)
  - Claim-based authorization (Department = IT)

- **Admin-only APIs**
  - Add/remove user claims via Identity APIs
  - Demonstrates operational user management

---

## Authentication Flow (High-Level)

1. User logs in with username/password
2. API validates credentials via ASP.NET Identity
3. API issues:
   - Short-lived JWT access token
   - Long-lived refresh token (stored server-side)
4. Client uses access token for API calls
5. When access token expires:
   - Client calls refresh endpoint
   - API reloads claims from Identity
   - New access + refresh tokens are issued
6. Logout revokes refresh tokens and prevents further refresh

---

## Why Refresh Tokens Are Stored in the Database

JWT access tokens are **stateless** and cannot be revoked once issued.

This project uses **database-backed refresh tokens** to:
- Allow secure logout
- Support token rotation
- Detect and prevent token reuse
- Reload claims after identity changes

This mirrors how many real-world APIs implement session control.

---

## Configuration & Secrets

### JWT Configuration

The JWT signing key is **not committed to source control**.

For local development, the key should be provided via an environment variable:

```bash
# Windows (PowerShell)
$env:Jwt__Key="YOUR_DEV_SECRET_KEY"

# Linux / macOS
export Jwt__Key="YOUR_DEV_SECRET_KEY"
```

The value in `appsettings.json` is a **non-functional placeholder** intended only to document required configuration.

### Database Configuration

The PostgreSQL connection string in `appsettings.json` is intended for **local development only** and points to a database running on `localhost`.

---

## Example Authorization Endpoints

- `GET /api/users/admin`  
  Requires **Admin role**

- `GET /api/users/it`  
  Requires claim `Department = IT`

- `POST /api/auth/login`  
  Issues access + refresh tokens

- `POST /api/auth/refresh`  
  Rotates refresh token and issues a new access token

- `POST /api/auth/logout`  
  Revokes refresh token(s)

- `POST /api/admin/users/{username}/claims`  
  Admin-only claim management

---

## Tech Stack

- ASP.NET Core Web API
- ASP.NET Identity
- Entity Framework Core
- PostgreSQL (Docker)
- JWT (HMAC SHA-256)

---

## Project Scope & Intent

This project intentionally focuses on:
- Backend security architecture
- Authentication and authorization correctness
- Real-world token lifecycles

It does **not** include:
- UI
- OAuth providers
- User self-registration

Those can be layered on top of this foundation.

---

## License

This project is licensed under the MIT License.

