# ArchoCybo - Complete Backend Code Generator Guide

## Overview

ArchoCybo is a powerful low-code/no-code platform for generating production-ready .NET 10 backend APIs. It's similar to Amplication but designed specifically for backend generation in C# .NET.

**Key Features:**
- User Registration & Multi-tenant Support
- Project-based Backend Generation
- Visual Entity Designer
- Multi-join Query Builder
- Automatic Code Generation
- ZIP Download & Deployment

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Authentication](#authentication)
3. [Project Management](#project-management)
4. [Entity Management](#entity-management)
5. [Query Builder](#query-builder)
6. [Code Generation](#code-generation)
7. [Download & Deploy](#download--deploy)
8. [Architecture](#architecture)
9. [File Structure](#file-structure)

---

## Getting Started

### Registration

1. Navigate to `/register`
2. Fill in your details:
   - Full Name
   - Email
   - Username
   - Password (minimum 8 characters)
   - Confirm Password

3. Click "Create Account"
4. You'll be redirected to login page

### Login

1. Navigate to `/login`
2. Enter your username and password
3. Click "Login"
4. You're now authenticated and can create projects

---

## Authentication

### How It Works

- JWT-based authentication
- Tokens are stored in the browser
- Automatic token attachment to API requests
- Secure logout with token cleanup

### Default Demo Account

```
Username: admin
Password: ChangeMe123!
```

---

## Project Management

### Creating a Project

1. Click **"My Projects"** in the navigation menu
2. Click **"New Project"** button
3. Fill in project details:

   **Project Name** (required)
   - Must be unique per user
   - Example: "UserManagementAPI"

   **Description** (optional)
   - Describes what your API does
   - Example: "User management system with role-based access"

   **Backend Framework**
   - .NET 10 (default, recommended)

   **Database Type**
   - SQL Server (default)
   - PostgreSQL
   - MySQL

   **Architecture Style**
   - **Clean Architecture** (recommended) - Best for scalability
   - **Layered Architecture** - Simpler structure
   - **CQRS** - Command Query Responsibility Segregation

   **Options**
   - Include JWT Authentication (checked by default)
   - Include Serialization (checked by default)

4. Click **"Create Project"**
5. You're now in the project detail page

### Project Statuses

- **Draft** - Project being configured
- **Ready** - All entities defined, ready to generate
- **Generated** - Code has been generated, ready to download

### Project Actions

- **Open** - Enter project to manage entities and queries
- **Download** - Download the generated backend ZIP file
- **Delete** - Permanently delete the project

---

## Entity Management

### What are Entities?

Entities represent database tables/collections in your API. Each entity:
- Becomes a database table
- Gets a corresponding C# class
- Automatically generates CRUD API endpoints
- Supports relationships with other entities

### Creating an Entity

1. Open a project
2. Go to **"Entities"** tab
3. Click **"Add Entity"**
4. Enter:

   **Entity Name** (required)
   - PascalCase recommended: "Product", "User", "Order"
   - Will be used for class name and API endpoint name

   **Plural Name** (optional)
   - Defaults to "EntityName + s"
   - Used for table/collection names
   - Example: "Products"

   **Description** (optional)
   - Documents the entity purpose

5. Click **"Create Entity"**

### Managing Entity Properties

1. In the **"Entities"** tab, find your entity
2. Click **"Manage"** button
3. This opens the property editor

#### Adding Properties

1. Click **"Add Property"**
2. Configure:

   **Property Name** (required)
   - PascalCase: "FirstName", "Email", "CreatedAt"

   **Data Type**
   - string
   - int
   - long
   - decimal
   - bool
   - DateTime
   - Guid

   **Nullable**
   - Check if property can be null

   **Required**
   - Check if property is required

   **Annotations** (optional)
   - `[Key]` - Primary key
   - `[Required]` - Required field
   - `[MaxLength(n)]` - String length limit
   - `[EmailAddress]` - Email validation

3. Click **"Add"**

#### Property Example: User Entity

```
Id (Guid, Primary Key, Auto-generated)
FirstName (string, Required, MaxLength 100)
LastName (string, Required, MaxLength 100)
Email (string, Required, EmailAddress)
Password (string, Required)
IsActive (bool, Default true)
CreatedAt (DateTime, Auto-generated)
UpdatedAt (DateTime, Nullable)
```

### Managing Relationships

Relationships define how entities connect to each other.

#### Relationship Types

**One-to-One**
- One User has One Profile
- Example: User ↔ Profile

**One-to-Many**
- One Category has Many Products
- Example: Category → Products

**Many-to-Many**
- Many Students attend Many Courses
- Example: Student ↔ Course (requires join table)

#### Adding a Relationship

1. In the **"Entities"** tab, find your entity
2. Click **"Add Relation"** button
3. Configure:

   **Relation Type**
   - One-to-One / One-to-Many / Many-to-Many

   **Target Entity**
   - Which entity to connect to

   **Foreign Key**
   - Property name for the foreign key
   - Example: "CategoryId"

   **Navigation Property**
   - Property name on current entity
   - Example: "Category" or "Products"

   **Join Table** (Many-to-Many only)
   - Table name for the junction table
   - Example: "StudentCourse"

4. Click **"Add"**

#### Relationship Examples

**User → Orders (One-to-Many)**
```
Source Entity: User
Target Entity: Order
Foreign Key: UserId
Navigation Property: Orders
```

**Product → Category (Many-to-One)**
```
Source Entity: Product
Target Entity: Category
Foreign Key: CategoryId
Navigation Property: Category
```

**Student ↔ Course (Many-to-Many)**
```
Source Entity: Student
Target Entity: Course
Relation Type: Many-to-Many
Join Table: StudentCourse
Navigation Properties: Courses / Students
```

---

## Query Builder

### What are Queries?

Queries define custom API endpoints that return specific data. They support:
- Multiple joins
- Filters
- Sorting
- Pagination
- Custom response DTOs

### Creating a Query

1. Open a project
2. Go to **"Queries"** tab
3. Click **"Create Query"**
4. Configure:

   **Query Name** (required)
   - Example: "GetProductsByCategory"
   - Will generate API endpoint: `GET /api/queries/GetProductsByCategory`

   **Source Entity**
   - The main entity being queried
   - Example: "Product"

#### Adding Joins

1. Click **"Add Join"**
2. Select:
   - **Join Type**: Inner, Left, Right, Full
   - **Target Entity**: Which table to join
   - **Join Condition**: The ON clause
   - Example: `Product.CategoryId = Category.Id`

3. Repeat for multiple joins

#### Adding Filters

1. Click **"Add Filter"**
2. Configure:
   - **Column**: Field to filter on
   - **Operator**: =, !=, >, <, LIKE, IN
   - **Value**: What to compare

3. Multiple filters are combined with AND logic

#### Example Query: Products by Category with Price Filter

```
Query Name: GetProductsByCategory
Source Entity: Product

Joins:
  INNER JOIN Category ON Product.CategoryId = Category.Id
  INNER JOIN Supplier ON Product.SupplierId = Supplier.Id

Filters:
  Category.Name = 'Electronics'
  Product.Price > 100
```

### Generated Query Endpoints

When you generate code, each query creates:

**API Endpoint**
```
GET /api/queries/GetProductsByCategory?categoryName=Electronics&minPrice=100&page=1&pageSize=20
```

**DTO Classes**
```csharp
public record GetProductsByCategoryDto(
    Guid ProductId,
    string ProductName,
    decimal Price,
    string CategoryName,
    string SupplierName
);

public record GetProductsByCategoryResultDto(
    List<GetProductsByCategoryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
```

**Service Method**
```csharp
public async Task<GetProductsByCategoryResultDto> GetProductsByCategoryAsync(
    string categoryName,
    decimal minPrice,
    int page,
    int pageSize
);
```

**Controller Method**
```csharp
[HttpGet("GetProductsByCategory")]
public async Task<IActionResult> GetProductsByCategory(
    [FromQuery] string categoryName,
    [FromQuery] decimal minPrice,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
{
    var result = await _queryService.GetProductsByCategoryAsync(
        categoryName, minPrice, page, pageSize);
    return Ok(result);
}
```

---

## Code Generation

### What Gets Generated?

For each entity, ArchoCybo generates:

#### Domain Layer
```
Domain/Entities/
  └─ YourEntity.cs
```

#### Application Layer
```
Application/DTOs/
  └─ YourEntityDtos.cs (Create, Update, Filter DTOs)

Application/Services/
  └─ IYourEntityService.cs (Interface)
  └─ YourEntityService.cs (Implementation)

Application/Interfaces/
  └─ IRepository.cs (Generic interface)
```

#### Infrastructure Layer
```
Infrastructure/Data/
  └─ AppDbContext.cs

Infrastructure/Repositories/
  └─ Repository.cs (Generic implementation)
```

#### Web API Layer
```
WebApi/Controllers/
  └─ YourEntityController.cs
```

#### Project Files
```
YourProject.csproj
Program.cs
appsettings.json
```

### Generated Code Features

**Entities**
- Auto-generated Guid primary keys
- CreatedAt/UpdatedAt timestamps
- Data annotations from properties
- Relationships

**DTOs**
- Create DTO (for POST requests)
- Update DTO (for PUT requests)
- Filter DTO (for GET queries)
- Response DTOs

**Services**
- GetAll()
- GetById(id)
- Create(dto)
- Update(id, dto)
- Delete(id)
- Search/Filter with pagination

**Controllers**
- GET / (GetAll)
- GET /{id} (GetById)
- POST / (Create)
- PUT /{id} (Update)
- DELETE /{id} (Delete)
- All endpoints are [Authorize]

**DbContext**
- All entities registered
- Automatic migrations support
- Foreign key relationships
- Navigation properties

---

## Download & Deploy

### Downloading Your Project

1. Open a project
2. Click **"Download Backend"** button
3. Browser downloads `project-backend.zip`

### Project File Structure

```
ProjectName-Backend.zip
├── Backend/
│   ├── Domain/
│   │   ├── Entities/
│   │   └── Enums/
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── Infrastructure/
│   │   ├── Data/
│   │   └── Repositories/
│   ├── WebApi/
│   │   └── Controllers/
│   ├── SharedKernel/
│   ├── Program.cs
│   ├── appsettings.json
│   └── YourProject.csproj
```

### Deploying the Generated Backend

#### Step 1: Extract and Open

```bash
unzip project-backend.zip
cd Backend
dotnet sln
# Open in Visual Studio
```

#### Step 2: Configure Database

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=sa;Password=YOUR_PASSWORD;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-at-least-32-characters-long",
    "ExpirationMinutes": 60
  }
}
```

#### Step 3: Create Database

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update
```

#### Step 4: Run the API

```bash
dotnet run
```

The API will be available at `https://localhost:5001`

#### Step 5: Test the API

Swagger documentation available at:
```
https://localhost:5001/swagger
```

### Example API Calls

**Create Entity**
```bash
curl -X POST https://localhost:5001/api/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Laptop",
    "price": 999.99,
    "categoryId": "guid-here"
  }'
```

**Get All**
```bash
curl -X GET https://localhost:5001/api/products \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Get By ID**
```bash
curl -X GET https://localhost:5001/api/products/{id} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Update**
```bash
curl -X PUT https://localhost:5001/api/products/{id} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "id": "guid-here",
    "name": "Updated Name",
    "price": 1099.99,
    "categoryId": "guid-here"
  }'
```

**Delete**
```bash
curl -X DELETE https://localhost:5001/api/products/{id} \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

---

## Architecture

### Design Pattern: Clean Architecture

```
WebApi (Presentation)
  ↓
Application (Business Logic)
  ↓
Infrastructure (Data Access)
  ↓
Domain (Core Entities)
  ↓
Database
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Framework | .NET 10 |
| API | ASP.NET Core |
| Database | SQL Server / PostgreSQL / MySQL |
| ORM | Entity Framework Core |
| Authentication | JWT Bearer |
| Validation | Data Annotations |
| Serialization | System.Text.Json |

### Key Patterns

1. **Repository Pattern** - Abstraction over data access
2. **Dependency Injection** - Loose coupling
3. **DTO Pattern** - Separation of concerns
4. **Service Pattern** - Business logic layer
5. **Entity Pattern** - Domain models

---

## File Structure

### Generated Project Layout

```
YourProject/
│
├── Domain/
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── Product.cs
│   │   └── Order.cs
│   └── Enums/
│       └── OrderStatus.cs
│
├── Application/
│   ├── DTOs/
│   │   ├── UserDtos.cs
│   │   ├── ProductDtos.cs
│   │   └── OrderDtos.cs
│   ├── Interfaces/
│   │   ├── IRepository.cs
│   │   ├── IUserService.cs
│   │   ├── IProductService.cs
│   │   └── IOrderService.cs
│   └── Services/
│       ├── UserService.cs
│       ├── ProductService.cs
│       └── OrderService.cs
│
├── Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs
│   └── Repositories/
│       └── Repository.cs
│
├── WebApi/
│   ├── Controllers/
│   │   ├── UserController.cs
│   │   ├── ProductController.cs
│   │   └── OrderController.cs
│   └── Properties/
│       └── launchSettings.json
│
├── SharedKernel/
│   └── (Shared utilities)
│
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── YourProject.csproj
```

---

## Best Practices

### Entity Design

1. Use PascalCase for entity names
2. Include CreatedAt and UpdatedAt timestamps
3. Use Guid for primary keys
4. Mark required properties with [Required]
5. Use appropriate data types

### Property Names

| Use Case | Example |
|----------|---------|
| Booleans | IsActive, HasPermission |
| Identifiers | UserId, OrderId |
| Dates | CreatedAt, UpdatedAt |
| Amounts | Price, TotalAmount |
| Text | Name, Description, Email |

### Query Naming

- **Get** - Retrieve data (GetProductsByCategory)
- **Count** - Return count (CountOrdersByUser)
- **Exists** - Check existence (ProductExists)
- **Search** - Full-text search (SearchProducts)

### Relationship Best Practices

1. Use consistent foreign key naming (EntityId)
2. Define navigation properties on both sides
3. Use cascade delete for owned entities
4. Use separate junction tables for many-to-many

---

## Troubleshooting

### Project Won't Generate

- Ensure all required fields are filled
- Check entity names are valid C# identifiers
- Verify no duplicate entity names

### Download Fails

- Check browser console for errors
- Ensure authenticated (valid JWT token)
- Try refreshing the page

### Generated Code Won't Compile

- Check .NET version (requires .NET 10)
- Verify all NuGet packages installed
- Check database connection string

### Database Migration Issues

```bash
# Remove last migration
dotnet ef migrations remove

# Create fresh migration
dotnet ef migrations add Initial

# Update database
dotnet ef database update
```

---

## Support & Resources

### Common Issues

**Authentication Fails**
- Verify JWT secret key in appsettings.json
- Check token expiration
- Ensure Authorization header format: `Bearer {token}`

**Database Connection Fails**
- Verify connection string
- Check database server is running
- Verify credentials

**API Returns 401 Unauthorized**
- Ensure you're sending valid JWT token
- Check token hasn't expired
- Verify [Authorize] attributes on controllers

---

## Next Steps

1. **Create Your First Project** - Go to "My Projects" and create
2. **Design Your Database** - Add entities and properties
3. **Set Up Relationships** - Connect entities together
4. **Define Custom Queries** - Add business logic queries
5. **Generate Code** - Create your backend
6. **Download & Deploy** - Get ZIP and deploy to production

Happy coding!
