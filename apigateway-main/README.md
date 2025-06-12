# API Gateway Service

## API Gateway Overview

The API Gateway serves as the central entry point for all microservices in the system. It handles authentication, request routing, and provides a unified API interface for frontend applications.

**Base URL:**

```
https://{your-host}/grp-7/api
```

---

### Features

- **Google OAuth 2.0 Authentication** - Secure user authentication using Google Identity Services
- **JWT Management** - Automatic token verification and user session handling
- **Service Proxying** - Routes authenticated requests to backend microservices
- **User Context Forwarding** - Adds user information headers to proxied requests
- **Development & Production Ready** - Configurable for different environments

---

## Authentication Endpoints

### 1. Google OAuth Login

```http
POST /login/google
Content-Type: application/json
```

**Request body:**

```json
{
  "credential": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE2N..."
}
```

* **credential** (string) – Google ID token from Google Identity Services

**Query Parameters:**
* `returnTo` (optional) – URL to redirect to after successful login (default: `/`)

**Responses**

* `302 Redirect` – Successful login, redirects to `returnTo` URL with auth cookies set
* `400 Bad Request` – Missing credential field
* `401 Unauthorized` – Invalid or expired Google ID token

**Set Cookies:**
* `id_token` (HttpOnly) – JWT for authentication (1 hour expiry)
* `display_name` – User's display name for UI purposes (1 hour expiry)

---

### 2. Get Current User

```http
GET /auth/me
Authorization: Bearer {token} (optional if cookie present)
```

**Responses**

* `200 OK` – Returns current user information:

  ```json
  {
    "user": {
      "iss": "https://accounts.google.com",
      "azp": "1033853639507-tfke0q2htjg2552a2stc0j1g3tsllnd3.apps.googleusercontent.com",
      "aud": "1033853639507-tfke0q2htjg2552a2stc0j1g3tsllnd3.apps.googleusercontent.com",
      "sub": "1234567890",
      "email": "user@example.com",
      "email_verified": true,
      "name": "John Doe",
      "picture": "https://lh3.googleusercontent.com/...",
      "given_name": "John",
      "family_name": "Doe",
      "iat": 1640995200,
      "exp": 1640998800
    }
  }
  ```

* `401 Unauthorized` – No valid authentication token provided

---

## Proxied Service Routes

All routes below require authentication and will automatically include user headers.

### Orders Service

```http
GET|POST/orders/*
```

Routes to: `http://order-service:3001/orders/*`

**Added Headers:**
* `x-user-email` – Authenticated user's email address
* `x-user-name` – Authenticated user's display name

---

## Health Check

### System Health

```http
GET /healthz
```

**Responses**

* `200 OK` – Returns "OK" if the gateway is running properly

---

## Authentication Flow

### Browser-based Applications

1. User visits a protected route
2. If not authenticated, user is redirected to `/login/google?returnTo={original-url}`
3. Frontend implements Google Identity Services to get ID token
4. Frontend POSTs the credential to `/login/google`
5. Gateway validates token and sets authentication cookies
6. User is redirected back to original URL
7. Subsequent requests use the `id_token` cookie automatically

### API/Mobile Applications

1. Application obtains Google ID token using Google Identity Services
2. Include token in `Authorization: Bearer {token}` header
3. Gateway validates token on each request
4. No cookies are set for API requests

---

## Environment Configuration

### Required Environment Variables

| Variable              | Description                           | Example                                    |
| --------------------- | ------------------------------------- | ------------------------------------------ |
| `GOOGLE_CLIENT_ID`    | Google OAuth 2.0 Client ID          | `1033853639507-example.apps.googleuser...` |
| `GOOGLE_CLIENT_SECRET`| Google OAuth 2.0 Client Secret      | `GOCSPX-ExampleSecret123`                  |
| `NODE_ENV`            | Environment mode                      | `development` or `production`              |
| `BASE_PATH`           | API base path prefix                  | `/grp-7/api`                              |

### Service Configuration

Services are configured via `/etc/config/services.json`:

```json
{
  "/orders": "http://order-service:3001/orders"
}
```

**Format:**
* **Key** – Route path that will be exposed by the gateway
* **Value** – Target service URL (including any base path)

---

## Development Setup

### Local Development

```bash
# Install dependencies
npm install

# Set environment variables
export GOOGLE_CLIENT_ID="your-google-client-id"
export GOOGLE_CLIENT_SECRET="your-google-client-secret"
export NODE_ENV="development"

# Start development server
npm run dev
```

### Docker Development

```bash
# Build and run with docker-compose
docker-compose up --build
```

**Docker Environment:**
* Gateway runs on port `8080`
* Mock order service runs on port `3001`
* Services configuration mounted from `./config/services.json`

---

## GitLab CI/CD Pipeline

## Pipeline Overview

This project uses a GitLab CI/CD pipeline with the following stages:

### build
- Compiles the TypeScript project and runs tests
- Triggered on every push (but not on tag-only pushes)

### build-image
- Builds and pushes a Docker image to the GitLab Container Registry
- Triggered **only on Git tag pushes** (e.g. `v1.0.0`)

### deployment
- Updates the deployment.yaml in the GitOps repository with the new Docker image tag
- Triggers ArgoCD to deploy the updated image to the cluster
- Triggered **only on Git tag pushes** (e.g. `v1.0.0`)

## Creating a Git Tag

To trigger the `build-image` and `deployment` stages, create and push a Git tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

---

## Security Features

### Token Validation
- All ID tokens are verified against Google's public keys
- Tokens are validated for correct audience (client ID)
- Expired tokens are automatically rejected

### Cookie Security
- `httpOnly` flag prevents XSS attacks on authentication cookies
- `secure` flag ensures cookies are only sent over HTTPS in production
- `sameSite=lax` provides CSRF protection while allowing normal navigation

### Request Security
- User information is automatically added to proxied requests
- No sensitive credentials are forwarded to backend services
- Each service receives only the necessary user context
