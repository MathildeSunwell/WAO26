# Order Tracking Service

## Order Tracking Service API

**Base URL:**

```
https://{your-host}/api/orders
```

---

### 1. Create a new order

```http
POST /api/orders
Content-Type: application/json
```

**Request body** – use the `OrderDto` schema:

```json
{
  "customerAddress": "Ryesgade 34, Aarhus C",
  "totalPrice": 37.50,
  "orderItems": [
    {
      "productName": "Classic Margherita",
      "size": "Large",
      "quantity": 2,
      "price": 12.50
    },
    {
      "productName": "BBQ Chicken",
      "size": "Medium",
      "quantity": 1,
      "price": 12.50
    }
  ]
}
```

* **customerAddress** (string) – delivery address
* **totalPrice** (decimal) – order total
* **orderItems** (array of objects) – each must include:

    * **productName** (string)
    * **size** (string)
    * **quantity** (int, default 0 if omitted)
    * **price** (decimal)

**Responses**

* `200 OK`

  ```text
  Order created with OrderID: 3fa85f64-5717-4562-b3fc-2c963f66afa6
  ```
* `400 Bad Request` – missing/invalid payload
* `500 Internal Server Error` – unexpected failure

---

### 2. Retrieve a single order

```http
GET /api/orders/{orderId}
```

* **Path parameter**

    * `orderId` (GUID) – the ID returned when you created the order.

**Responses**

* `200 OK` – returns an `OrderResponseDto` JSON payload:

  ```json
  {
    "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "orderStatus": "Pending",
    "paymentStatus": "Pending",
    "restaurantStatus": "Pending",
    "deliveryStatus": "Pending",
    "createTime": "2025-05-25T14:30:00Z",
    "lastUpdated": "2025-05-25T14:30:00Z",
    "comment": ""
  }
  ```

* `400 Bad Request` – empty or invalid GUID

* `404 Not Found` – no order with that ID

* `500 Internal Server Error` – unexpected failure

---

### 3. List / filter orders

```http
GET /api/orders?[query-parameters]
```

Supports these **optional** query parameters:

| Parameter          | Type     | Description                                                                               |
| ------------------ | -------- | ----------------------------------------------------------------------------------------- |
| `orderStatus`      | string   | exact match; one of: `Pending`, `Processing`, `Completed`, `Cancelled`                    |
| `paymentStatus`    | string   | exact match; one of: `Pending`, `Reserved`, `Failed`, `Succeeded`, `Cancelled`            |
| `restaurantStatus` | string   | exact match; one of: `Pending`, `Accepted`, `Rejected`, `Ready`, `Completed`, `Cancelled` |
| `deliveryStatus`   | string   | exact match; one of: `Pending`, `Assigned`, `Started`, `Completed`, `Cancelled`           |
| `createdAfter`     | DateTime | ISO-8601 timestamp; only orders on/after this moment                                      |
| `createdBefore`    | DateTime | ISO-8601 timestamp; only orders on/before this moment                                     |
| `page`             | int      | 1-based page number; default `1`                                                          |
| `pageSize`         | int      | items per page; default `20`                                                              |

**Example request**

```
GET /api/orders?
    paymentStatus=Reserved&
    deliveryStatus=Assigned&
    createdAfter=2025-05-01T00:00:00Z&
    page=2&pageSize=10
```

**Response**

* `200 OK` – JSON array of `OrderResponseDto`, ordered by `createTime` **descending** (newest first), paged.
* `500 Internal Server Error` – unexpected failure

---

### DTO Definitions

#### `OrderDto` (for POST)

```csharp
public class OrderDto
{
    public string CustomerAddress { get; set; }
    public required decimal TotalPrice { get; set; }
    public required List<OrderItemDto> OrderItems { get; set; } = new();
}

public class OrderItemDto
{
    public required string ProductName { get; set; }
    public required string Size { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

#### `OrderResponseDto` (returned by GET)

```csharp
public class OrderResponseDto
{
    public Guid   OrderId           { get; set; }
    public string OrderStatus       { get; set; }        // Pending | Processing | Completed | Cancelled
    public string PaymentStatus     { get; set; }        // Pending | Reserved | Failed | Succeeded | Cancelled
    public string RestaurantStatus  { get; set; }        // Pending | Accepted | Rejected | Ready | Completed | Cancelled
    public string DeliveryStatus    { get; set; }        // Pending | Assigned | Started | Completed | Cancelled
    public DateTime CreateTime      { get; set; }
    public DateTime LastUpdated     { get; set; }
}

```

---

# GitLab CI/CD Pipeline

## Pipeline Overview

This project uses a GitLab CI/CD pipeline with the following stages:

### 🔨 build
- Compiles the `OrderTrackingService` project
- Triggered on every push (but not on tag-only pushes)

### 🐳 build-image
- Builds and pushes a Docker image to the GitLab Container Registry
- Triggered **only on Git tag pushes** (e.g. `v1.0.0`)

### 🚀 deployment
- Updates the deployment.yaml in the GitOps repository with the new Docker image tag.
- Triggers ArgoCD to deploy the updated image to the cluster.
- Triggered **only on Git tag pushes** (e.g. `v1.0.0`)

## 🏷️ Creating a Git Tag

To trigger the `build-image` stage, create and push a Git tag:

```bash
git tag v1.0
git push origin v1.0
