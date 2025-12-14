# ArchoCybo - Quick Start Guide

## 🚀 30-Second Overview

ArchoCybo is a **low-code backend code generator** for .NET 10. In 4 clicks:
1. Create a project
2. Design your database entities
3. Download complete backend code
4. Deploy & run

---

## Getting Started

### 1. Start the Application

```bash
cd ArchoCybo
dotnet run
```

Visit: `https://localhost:7000`

### 2. Register or Login

**New User:**
- Click "Create Account" on login page
- Fill in details and register
- Redirects to login automatically

**Demo Account:**
```
Username: admin
Password: ChangeMe123!
```

### 3. Create Your First Project

1. Click **"My Projects"** in the menu
2. Click **"New Project"** button
3. Fill in:
   - **Project Name**: e.g., "UserAPI"
   - **Description**: What it does
   - **Database**: SQL Server (default)
   - **Architecture**: Clean Architecture (default)
4. Click **"Create Project"**

### 4. Design Your Database

#### Add an Entity
1. Open your project
2. Go to **"Entities"** tab
3. Click **"Add Entity"**
4. Enter entity name: e.g., "User"
5. Click **"Create Entity"**

#### Add Properties
1. Find entity in table
2. Click **"Manage"**
3. Click **"Add Property"**
4. Fill in:
   - **Name**: e.g., "Email"
   - **Type**: string
   - **Required**: Check it
   - **Annotations**: [EmailAddress]
5. Click **"Add"**

#### Add Relationships
1. In Entities tab, click **"Add Relation"**
2. Choose:
   - **Type**: One-to-Many
   - **Target**: Another entity
   - **Foreign Key**: e.g., "CategoryId"
3. Click **"Add"**

### 5. Generate Code

1. Click **"Download Backend"** button
2. ZIP file downloads automatically
3. Extract and open in Visual Studio

### 6. Deploy

```bash
# Open the extracted project folder
cd Backend

# Edit database connection
# Edit appsettings.json with your DB connection string

# Create database
dotnet ef database update

# Run the API
dotnet run

# Test with Swagger
# Open: https://localhost:5001/swagger
```

---

## What Gets Generated?

For each entity (e.g., Product):

```
✅ Product.cs (Entity class)
✅ ProductDtos.cs (Create, Update, Filter DTOs)
✅ IProductService.cs + ProductService.cs (Business logic)
✅ ProductController.cs (API endpoints)
✅ Repository.cs (Data access)
✅ AppDbContext.cs (Database context)
✅ Program.cs (Dependency injection)
```

### Generated API Endpoints

```
GET    /api/product              → Get all products
GET    /api/product/{id}         → Get product by ID
POST   /api/product              → Create product
PUT    /api/product/{id}         → Update product
DELETE /api/product/{id}         → Delete product
```

---

## Example: Product Inventory

### Step 1: Create Project
- Name: "InventoryAPI"
- Database: SQL Server

### Step 2: Create Entities
- **Product**: Name, Price, Stock
- **Category**: Name
- **Relationship**: Product → Category (Many-to-One)

### Step 3: Generate
- Click "Download Backend"

### Step 4: Run
```bash
unzip InventoryAPI-Backend.zip
cd Backend
dotnet ef database update
dotnet run
```

### Step 5: Test API
```bash
# Create category
curl -X POST https://localhost:5001/api/category \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name": "Electronics"}'

# Get all products
curl -X GET https://localhost:5001/api/product \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Common Tasks

### Change Database Connection
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=sa;Password=YOUR_PASSWORD;"
  }
}
```

### Add New Entity to Generated Project
You can manually add entities to the generated project (same structure as generated ones).

### Add Custom Business Logic
Edit the generated Service classes to add custom logic.

### Customize API Endpoints
Edit the generated Controller classes to customize behavior.

---

## Troubleshooting

### Can't login?
- Make sure you registered first
- Check username and password spelling
- Try demo account: `admin / ChangeMe123!`

### Project won't generate?
- Ensure you added at least one entity
- Check entity names are valid (no spaces)
- Refresh the page

### Generated code won't run?
- Verify .NET 10 is installed: `dotnet --version`
- Check database connection string
- Run: `dotnet restore`

### Database migration fails?
```bash
# Clear migrations
dotnet ef migrations remove
dotnet ef migrations add Initial
dotnet ef database update
```

---

## Features

✅ **Visual Database Designer** - No SQL needed
✅ **Code Generation** - Create C# files automatically
✅ **Entity Relationships** - One-to-One, One-to-Many, Many-to-Many
✅ **JWT Authentication** - Secure API endpoints
✅ **Clean Architecture** - Production-ready structure
✅ **Entity Framework Core** - ORM integrated
✅ **Dependency Injection** - Built-in DI container
✅ **DTOs** - Automatic data transfer objects
✅ **Repositories** - Generic repository pattern
✅ **Services** - Business logic layer

---

## Learn More

📖 **Full Documentation**: See `BACKEND_GENERATOR_GUIDE.md`
📋 **System Overview**: See `COMPLETE_SYSTEM_SUMMARY.md`
🔐 **Auth & Security**: See `IMPLEMENTATION_GUIDE.md`

---

## Support

**Issue with generated code?**
- Check code examples in `BACKEND_GENERATOR_GUIDE.md`
- Review generated code structure
- Follow .NET best practices

**Feature request?**
- Add more entity types
- Custom code generation templates
- Export options (JSON, SQL)

---

**Happy coding! 🚀**

You can now generate complete backend APIs in minutes instead of days!
