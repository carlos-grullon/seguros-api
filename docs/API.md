# Seguros API — Endpoint Documentation

Base URL (development): `http://localhost:5033`

---

## Authentication Endpoints

### 1. Register

Creates a new user with one of the allowed roles: **Admin**, **Client**, or **Agent**.

| | |
|---|---|
| **Method** | `POST` |
| **URL** | `/api/auth/register` |
| **Auth** | None |
| **Content-Type** | `application/json` |

#### Request body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | Yes | Valid email address (max 256 chars). Must be unique. |
| `password` | string | Yes | At least 6 characters. |
| `firstName` | string | Yes | Max 100 characters. |
| `lastName` | string | Yes | Max 100 characters. |
| `roleName` | string | Yes | One of: `Admin`, `Client`, `Agent`. |

#### Example request

```http
POST http://localhost:5033/api/auth/register
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "SecurePass123",
  "firstName": "John",
  "lastName": "Doe",
  "roleName": "Admin"
}
```

#### Example response — 200 OK

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@example.com",
  "roleName": "Admin",
  "firstName": "John",
  "lastName": "Doe",
  "expiresAt": "2025-02-19T12:00:00Z"
}
```

#### Example response — 400 Bad Request (invalid role)

```json
{
  "message": "RoleName must be one of: Admin, Client, Agent."
}
```

#### Example response — 400 Bad Request (email in use)

```json
{
  "message": "Registration failed. Email may already be in use or role is invalid."
}
```

---

### 2. Login

Authenticates a user and returns a JWT and user info.

| | |
|---|---|
| **Method** | `POST` |
| **URL** | `/api/auth/login` |
| **Auth** | None |
| **Content-Type** | `application/json` |

#### Request body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | string | Yes | User's email. |
| `password` | string | Yes | User's password. |

#### Example request

```http
POST http://localhost:5033/api/auth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "SecurePass123"
}
```

#### Example response — 200 OK

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@example.com",
  "roleName": "Admin",
  "firstName": "John",
  "lastName": "Doe",
  "expiresAt": "2025-02-19T12:00:00Z"
}
```

#### Example response — 401 Unauthorized

```json
{
  "message": "Invalid email or password."
}
```

#### Using the token

For endpoints that require authentication, send the JWT in the header:

```http
Authorization: Bearer <token>
```

---

## Other Endpoints
---

### 3. Get Current User (Me)

Returns the authenticated user's profile information.

| | |
|---|---|
| **Method** | `GET` |
| **URL** | `/api/auth/me` |
| **Auth** | Bearer JWT |

#### Example request

```http
GET http://localhost:5033/api/auth/me
Authorization: Bearer <token>
```

#### Example response — 200 OK

```json
{
  "userId": 1,
  "email": "admin@example.com",
  "roleName": "Admin",
  "firstName": "John",
  "lastName": "Doe"
}
```

## Quick reference

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/register` | POST | Register Admin, Client, or Agent user |
| `/api/auth/login` | POST | Login and get JWT |
| `/api/auth/me` | GET | Current authenticated user |
