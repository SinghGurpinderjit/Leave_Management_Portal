# Leave Management System

A distributed microservices-based Leave Management System built with **.NET 8**, demonstrating modern cloud-native architecture patterns including API Gateway, Service Discovery, Event-Driven Communication, Distributed Tracing, and Containerization.

---

## Features

* Microservices Architecture
* API Gateway using Ocelot
* Service Discovery with Eureka
* Event-Driven Communication using RabbitMQ
* PostgreSQL Database
* Distributed Tracing with OpenTelemetry & Jaeger
* Docker & Docker Compose Support
* Multiple Service Instances for Load Balancing

---

## Architecture Components

| Component                | Purpose                                  |
| ------------------------ | ---------------------------------------- |
| API Gateway              | Single entry point for client requests   |
| User Service             | User management and employee creation    |
| Leave Management Service | Leave application and approval workflows |
| Notification Service     | Event-based notifications                |
| RabbitMQ                 | Asynchronous messaging                   |
| Eureka                   | Service discovery                        |
| PostgreSQL               | Data persistence                         |
| Jaeger                   | Distributed tracing                      |

---

## Inter-Service Communication

The system uses both synchronous and asynchronous communication.

### Synchronous (HTTP)

Client
   │
   ▼
API Gateway
   │
   ▼
Target Service

Used for:
* User validation
* Employee lookup
* Leave information retrieval

### Asynchronous (RabbitMQ)
RabbitMQ UI:
http://localhost:15672/

1) User Created Event
User Service
      │
      ▼
Publish user.created
      │
      ▼
RabbitMQ Exchange
   │             
   ▼             
Leave Service 

2) Leave Status Update Event
Leave Service
      │
      ▼
Publish leave.statusupdated
      │
      ▼
RabbitMQ Exchange
   │             
   ▼             
Notification Service

## Distributed Tracing

OpenTelemetry propagates trace context through RabbitMQ headers, allowing complete request tracking in Jaeger.

Example flow:

Client
   │
API Gateway
   │
User Service
   │
RabbitMQ Publish
   │
RabbitMQ Consume
   │
Leave Management Service

Jaeger UI:
http://localhost:16686

---

## Prerequisites

* Docker Desktop
* Docker Compose
* Git

Verify installation:

docker --version
docker compose version

## Clone Repository

git clone <repository-url> // repository url: https://github.com/SinghGurpinderjit/Leave_Management_Portal.git
cd <repository-folder> // cd EmployeeLeaveManagementPortal

## Run the Application

1) Open cmd and go to docker-compose.yml file path
2) Run below command to build all services:
   docker compose up --build
3) Run below command to start all services in detached mode:
   docker compose up -d

You can stop all running application services using below command:
   - docker compose down

## Access URLs

| Service             | URL                    |
| ------------------- | ---------------------- |
| API Gateway         | http://localhost:5000  |
| Eureka Dashboard    | http://localhost:8761  |
| RabbitMQ Management | http://localhost:15672 |
| Jaeger UI           | http://localhost:16686 |
| PostgreSQL          | localhost:5431         |

### RabbitMQ Credentials
Username: guest
Password: guest

## Environment Configuration

The project is fully configured through the provided "docker-compose.yml".

Major configurations include:
* PostgreSQL Connection Strings
* RabbitMQ Host and Credentials
* Eureka Service Discovery
* Jaeger OTLP Endpoint
* ASP.NET Core Environment

No additional `.env` file is required for local setup.

## API Testing

All APIs should be accessed through the API Gateway.
Base URL: http://localhost:5000

Suggested testing flow:
1. Create User
2. Verify automatic leave balance initialization
3. Apply Leave
4. Approve/Reject Leave
5. Verify RabbitMQ event processing
6. View end-to-end traces in Jaeger

API testing tools:
* Postman
* Swagger (if enabled)
* REST Client

## Docker Compose

The attached "docker-compose.yml" provisions the complete environment:

### Infrastructure
* Eureka Server
* RabbitMQ
* PostgreSQL
* Jaeger

### Application Services
* API Gateway
* User Service (2 Instances)
* Leave Management Service (2 Instances)
* Notification Service (2 Instances)

## Technology Stack
* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* PostgreSQL
* RabbitMQ
* Ocelot API Gateway
* Steeltoe Eureka
* OpenTelemetry
* Jaeger
* Serilog
* Docker
* Docker Compose

## Project Highlights
* API Gateway Pattern
* Service Discovery
* Event-Driven Architecture
* Publisher/Subscriber Messaging
* Distributed Tracing
* Containerized Deployment
* Horizontal Scaling
* Clean Architecture Principles

## Notes

* All services register automatically with Eureka.
* RabbitMQ exchanges and queues are created during startup.
* Jaeger traces include both HTTP requests and RabbitMQ message flows.
* Multiple service instances demonstrate load balancing and scalability.

## Appendix

Attach the complete "docker-compose.yml" file below this section for deployment and execution.
