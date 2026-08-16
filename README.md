# .NET 9 Minimal Web API - Best Practices Showcase

A showcase project built with **.NET 9 Minimal APIs**, demonstrating idiomatic architecture, **SQLite with Entity Framework Core 9**, **Scalar UI** (without Swagger/Swashbuckle), **`TypedResults`**, **FluentValidation with Endpoint Filters**, and **RFC 7807 Problem Details**.

---

## 🌟 Highlights & Features

- **Target Framework**: `.NET 9` (`net9.0`)
- **Interactive Documentation**: **Scalar UI** via `Scalar.AspNetCore` at `/scalar/v1` (no Swashbuckle/Swagger).
- **Native OpenAPI**: Microsoft's official `Microsoft.AspNetCore.OpenApi` with document transformers.
- **Local Persistence**: **SQLite** database (`catalog.db`) using **Entity Framework Core 9** with automatic schema creation and rich initial data seeding.
- **Strongly Typed Responses**: Compile-time `TypedResults` (`Ok<T>`, `CreatedAtRoute<T>`, `ValidationProblem`, `ProblemHttpResult`, `NoContent`).
- **Endpoint Filters**:
  - `ValidationFilter<T>` using `FluentValidation` for clean declarative validation before reaching endpoint handlers.
  - `RequestTimingFilter` for measuring response execution time and injecting `X-Response-Time-Ms` headers.
- **Modular Route Groups**: Clean separation with `app.MapGroup()` and dedicated extension methods (`ProductEndpoints`, `CategoryEndpoints`, `ReviewEndpoints`, `AnalyticsEndpoints`, `SystemEndpoints`).
- **Global Error Handling**: Standard .NET 8/9 `IExceptionHandler` middleware emitting RFC 7807 `ProblemDetails`.
- **Relational Domain**: Products, Categories, Customer Reviews with aggregate calculations, stock level tracking, and full financial valuation.
- **Diagnostics**: Built-in Health Checks (`/health`) with SQLite connection probes.

---

## 🏗️ Project Structure

```
├── DotnetMinimalApi.sln
├── DotnetMinimalApi.http                 # Interactive HTTP requests file for all endpoints
├── README.md
└── src/
    └── DotnetMinimalApi/
        ├── DotnetMinimalApi.csproj
        ├── Program.cs                    # Application configuration, DI & pipeline
        ├── appsettings.json              # Connection strings and configuration
        ├── appsettings.Development.json
        ├── Common/
        │   ├── Exceptions/
        │   │   ├── GlobalExceptionHandler.cs  # RFC 7807 ProblemDetails IExceptionHandler
        │   │   ├── ResourceNotFoundException.cs
        │   │   └── ConflictException.cs
        │   ├── Filters/
        │   │   ├── ValidationFilter.cs        # Generic FluentValidation Endpoint Filter
        │   │   └── RequestTimingFilter.cs     # Performance monitoring filter
        │   └── Pagination/
        │       ├── PagedList.cs               # Pagination metadata and items wrapper
        │       └── PaginationParams.cs        # Reusable query parameter record
        ├── Data/
        │   ├── AppDbContext.cs                # EF Core DbContext with model mapping
        │   └── DbInitializer.cs               # Automatic creation and realistic seed data
        ├── Models/
        │   ├── Entities/                      # Domain database models
        │   │   ├── BaseEntity.cs
        │   │   ├── Category.cs
        │   │   ├── Product.cs
        │   │   └── Review.cs
        │   └── Dtos/                          # Immutable C# records for I/O
        │       ├── ProductDtos.cs
        │       ├── CategoryDtos.cs
        │       ├── ReviewDtos.cs
        │       └── AnalyticsDtos.cs
        ├── Validation/                        # FluentValidation rules
        │   ├── ProductValidators.cs
        │   ├── CategoryValidators.cs
        │   └── ReviewValidators.cs
        └── Endpoints/                         # Modular Minimal API route groups
            ├── ProductEndpoints.cs
            ├── CategoryEndpoints.cs
            ├── ReviewEndpoints.cs
            ├── AnalyticsEndpoints.cs
            └── SystemEndpoints.cs
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Running the API

```bash
# Clone or navigate to the directory
cd /home/admin/claudefree

# Run the project (default ports: http://localhost:5000)
dotnet run --project src/DotnetMinimalApi
```

### Accessing Scalar UI
Open your browser and navigate to:
```
http://localhost:5000/scalar/v1
```
*(Navigating to `/` will automatically redirect to `/scalar/v1`)*

---

## 📡 API Endpoints Overview

### 🏷️ Categories (`/api/categories`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/categories` | Get all categories with product counts |
| `GET` | `/api/categories/{id}` | Get category by ID with valuation & top product previews |
| `GET` | `/api/categories/{id}/products` | Get paginated products belonging to category |
| `POST` | `/api/categories` | Create new category (auto slug generation) |
| `PUT` | `/api/categories/{id}` | Update existing category |
| `DELETE` | `/api/categories/{id}` | Delete category (checks for child products) |

### 📦 Products (`/api/products`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/products` | Paginated product search, price filter & sorting |
| `GET` | `/api/products/{id}` | Get product details with review averages |
| `GET` | `/api/products/sku/{sku}` | Get product by unique SKU |
| `POST` | `/api/products` | Create product (validated via FluentValidation) |
| `PUT` | `/api/products/{id}` | Update product details |
| `DELETE` | `/api/products/{id}` | Delete product and reviews |
| `PATCH` | `/api/products/{id}/stock` | Adjust stock quantity (+/- adjustment) |
| `PATCH` | `/api/products/{id}/status` | Toggle product active status |

### ⭐ Reviews (`/api/products/{productId}/reviews` & `/api/reviews`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/products/{productId}/reviews` | Get customer reviews and average rating |
| `POST` | `/api/products/{productId}/reviews` | Submit a customer review (1 to 5 stars) |
| `DELETE` | `/api/reviews/{id}` | Remove a customer review |

### 📊 Analytics & Reports (`/api/analytics`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/analytics/summary` | Real-time financial inventory valuation & metrics |
| `GET` | `/api/analytics/low-stock` | Products at or below low-stock threshold |

### ⚙️ System & Health (`/api/system` & `/health`)
| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/health` | Application liveness and SQLite connectivity probe |
| `GET` | `/api/system/info` | Runtime environment and server metadata |
| `POST` | `/api/system/reset-and-seed` | Reset and reseed SQLite database |

---

## 🛡️ Microsoft Best Practices Implemented

1. **TypedResults**: No untyped `IResult` returns. All handlers specify concrete return types such as `Results<Ok<T>, ProblemHttpResult, ValidationProblem>` ensuring 100% accurate OpenAPI documentation and compile-time verification.
2. **Endpoint Filters for Validation**: Requests are validated before reaching the business handler using `ValidationFilter<T>`, keeping endpoints concise and expressive.
3. **Problem Details (RFC 7807)**: Centralized error responses conforming to standard HTTP problem details via `IExceptionHandler`.
4. **Asynchronous & Cancellation Aware**: All database and I/O queries accept `CancellationToken` and utilize `AsNoTracking()` for high-throughput reads.
5. **No Swashbuckle**: In alignment with .NET 9 recommendations, OpenAPI specs are produced by `Microsoft.AspNetCore.OpenApi` and visualized using `Scalar.AspNetCore`.
