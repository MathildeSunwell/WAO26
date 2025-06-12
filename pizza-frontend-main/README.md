# Order Frontend

## Restaurant Order Management Frontend

**Base URL:**
```
https://swwao-courses.orbit.au.dk/grp-7/frontend
```

**API Base URL:**
```
https://swwao-courses.orbit.au.dk/grp-7/api
```

---

## Features

### Authentication
- **Google OAuth Integration** - Users can sign in using their Google accounts
- **Session Management** - Persistent login sessions with secure cookie handling
- **Protected Routes** - Automatic redirect to login for unauthenticated users

### Order Management
- **Create Orders** - Interactive form to place new restaurant orders
- **View Orders** - List all orders with search and filtering capabilities
- **Order Details** - Detailed view of individual orders with status tracking
- **Real-time Status** - Live updates on order, payment, restaurant, and delivery status

### User Interface
- **Responsive Design** - Works seamlessly on desktop and mobile devices
- **Modern Styling** - Clean, professional interface with CSS custom properties
- **Navigation** - Intuitive navigation between different sections

---

## Pages & Components

### Home Page (`/`)
- Welcome dashboard for authenticated users
- Login interface for unauthenticated users
- Quick access to main functionality

### Create Order (`/orders/new`)
```typescript
// Order creation form with fields:
{
  customerAddress: string;    // Delivery address
  productName: string;        // Item name
  size: string;              // Product size
  quantity: number;          // Item quantity
  price: number;             // Price per item
}
```

### Orders List (`/orders`)
- Display all user orders
- Search functionality by customer address
- Quick links to individual order details

### Order Details (`/orders/[id]`)
```typescript
// Displays comprehensive order information:
{
  orderId: string;
  customerAddress: string;
  orderStatus: string;       // Pending | Processing | Completed | Cancelled
  paymentStatus: string;     // Pending | Reserved | Failed | Succeeded | Cancelled
  restaurantStatus: string;  // Pending | Accepted | Rejected | Ready | Completed | Cancelled
  deliveryStatus: string;    // Pending | Assigned | Started | Completed | Cancelled
  createTime: string;
  lastUpdated: string;
}
```

---

## Technical Architecture

### Framework & Technology Stack
- **Next.js 15.3.2** - React framework with App Router
- **React 19.1.0** - Latest React with modern features
- **TypeScript** - Type-safe development
- **CSS Custom Properties** - Modern styling approach

### Configuration
```typescript
// next.config.ts
{
  basePath: "/grp-7/frontend",
  output: "standalone",
  rewrites: [
    {
      source: "/api/:path*",
      destination: "https://swwao-courses.orbit.au.dk/grp-7/api/:path*"
    }
  ]
}
```

### Environment Variables
```env
NODE_ENV=development
REWRITE_DESTINATION="https://swwao-courses.orbit.au.dk/grp-7/api/:path*"
NEXT_PUBLIC_GOOGLE_CLIENT_ID=1033853639507-tfke0q2htjg2552a2stc0j1g3tsllnd3.apps.googleusercontent.com
NEXT_PUBLIC_BASE_PATH=/grp-7/frontend
```

### Security Features
- **CORS Configuration** - Proper cross-origin policy settings
- **Secure Headers** - Cross-Origin-Opener-Policy and Cross-Origin-Embedder-Policy
- **Credential Handling** - Secure cookie management for authentication

---

## GitLab CI/CD Pipeline

### Pipeline Overview

This project uses a GitLab CI/CD pipeline with the following stages:

### build
- Installs dependencies with `npm ci`
- Builds the Next.js application with `npm run build`
- Triggered on every push (but not on tag-only pushes)

### build-image
- Builds and pushes a Docker image to the GitLab Container Registry
- Uses multi-stage Docker build for optimized production images
- Triggered **only on Git tag pushes** (e.g. `v1.0.0`)

### deployment
- Updates the deployment.yaml in the GitOps repository with the new Docker image tag
- Modifies the container image reference using `yq`
- Triggers ArgoCD to deploy the updated image to the cluster
- Triggered **only on Git tag pushes** (e.g. `v1.0.0`)

## Creating a Git Tag

To trigger the `build-image` and `deployment` stages, create and push a Git tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

---

## Development

### Setup
```bash
# Install dependencies
npm ci

# Start development server
npm run dev

# Build for production
npm run build

# Start production server
npm start

# Run linting
npm run lint
```

---

## API Integration

The frontend communicates with the Order Tracking Service API through:

- **Authentication Endpoints** - `/api/auth/me`, `/api/login/google`
- **Order Management** - `/api/orders` (GET, POST), `/api/orders/{id}` (GET)
- **Automatic Rewrites** - All `/api/*` requests are proxied to the backend service
