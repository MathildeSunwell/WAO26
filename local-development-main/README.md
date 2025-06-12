# Local Development Stack

A minimal Docker Compose setup to run the key services needed for local microservice development. This includes MongoDB, SQL Server, and RabbitMQ. All UI logins are admin:admin.

## Quick Start

1. **Clone** this repo:

   ```bash
   git clone <repo-url> && cd <repo-dir>
   ```

2. **Start** all services:

   ```bash
   docker-compose up
   ```

3. **Add** the network configuration in your microservice docker compose:

   ```yaml
   networks:
     default:
       name: local-dev-net
       external: true
   ```

## Services & Access

- **MongoDB** (`mongo:8.0.9`)

  - **Host:** `localhost:27017`
  - **Credentials:** `root` / `admin`
  - **Connection String:**

    ```
    mongodb://root:admin@localhost:27017
    ```

- **Mongo Express** (`mongo-express:1.0.2-20-alpine3.19`)

  - **UI:** `http://localhost:8081`
  - **Login:** `admin` / `admin`

- **SQL Server** (`mcr.microsoft.com/mssql/server:2019-latest`)

  - **Host:** `localhost:1433`
  - **Credentials:** `sa` / `Admin1234!`
  - **Connection String:**

    ```
    Server=localhost,1433;Database=master;User Id=sa;Password=Admin1234!;
    ```

- **SQLPad** (`sqlpad/sqlpad:latest`)

  - **UI:** `http://localhost:4000`
  - **Login:** `admin` / `admin`

- **RabbitMQ** (`rabbitmq:3.12-management-alpine`)

  - **AMQP URL:**

    ```
    amqp://user:admin@localhost:5672
    ```

  - **Management UI:** `http://localhost:15672`
  - **Login:** `admin` / `admin`

## Networking

By default, Compose will create (or reuse) a bridge network called `local-dev-net` and attach all services to it. To make other Compose projects join this same network, add the following to **their** `docker-compose.yml`:

```yaml
networks:
  default:
    name: local-dev-net
    external: true
```

## Data Persistence

Volumes (`mongo_data`, `sqlserver_data`, `sqlpad_data`) store data so containers can restart without data loss.

## Cleanup

- **Stop** containers & remove volumes **(data lost)**:

  ```bash
  docker-compose down --volumes
  ```
