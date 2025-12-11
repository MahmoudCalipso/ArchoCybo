# ArchoCybo - Implementation Guide

## Overview

Your Blazor Server application has been enhanced with:

1. **Proper Authentication & Authorization** - Login required for all pages except the login page
2. **Advanced Query Builder** - Build queries with visual tools and generate complete code
3. **Entity Manager** - Full CRUD operations, relationships, and annotations (Amplication-like)

---

## Authentication & Authorization

### How It Works

- **Login Required**: All pages now require authentication except `/login`
- **JWT Authentication**: Uses JWT tokens for secure authentication
- **Auth State Provider**: Manages authentication state across the app
- **Auto Redirect**: Unauthenticated users are automatically redirected to login

### Default Credentials

```
Username: admin
Password: ChangeMe123!
```

### Key Files

- `ArchoCybo/Services/AuthStateProvider.cs` - Manages authentication state
- `ArchoCybo/Pages/Login.razor` - Login page with improved UI
- `ArchoCybo/Program.cs` - Registers authentication services
- `ArchoCybo/Shared/MainLayout.razor` - Shows auth state in navigation

### Usage

1. Start the application
2. You'll be redirected to `/login`
3. Enter credentials and click Login
4. Once authenticated, you can access all pages
5. Click Logout to sign out

---

## Advanced Query Builder

### Location
Navigate to: **Query Builder (Advanced)** from the menu

### Features

#### 1. Visual Query Building
- Select entity from dropdown
- Choose specific columns to include
- Add filters with operators (=, !=, >, <, LIKE, IN)
- Build complex queries visually

#### 2. Query Testing
- Click "Build Query" to generate SQL
- View generated SQL in syntax-highlighted panel
- Click "Test Query" to execute against database
- See results in dynamic table

#### 3. Code Generation
Click "Generate Code" to create complete application code:

**Generated Files:**
- **DTOs**: Data Transfer Objects with filter DTOs
- **Repository**: Interface and implementation with search methods
- **Service**: Full CRUD service with paging
- **Controller**: API controller with all endpoints

**Generated Code Includes:**
- Proper separation of concerns
- Generic paging support
- Search/filter functionality
- Standard CRUD operations
- Authorization attributes

#### 4. Copy Functionality
- Copy SQL query to clipboard
- Copy all generated code at once
- Separate tabs for each code file

### Example Workflow

1. Select entity (e.g., "User")
2. Choose columns (Id, Username, Email)
3. Add filter: Email LIKE "admin"
4. Click "Build Query" → See SQL
5. Click "Test Query" → See results
6. Click "Generate Code" → Get full code
7. Copy and use in your project

---

## Entity Manager

### Location
Navigate to: **Entity Manager** from the menu

### Features

#### 1. Entity Management
- View all entities in the system
- Search entities by name
- Create new entities
- View entity details

#### 2. Property Management
- Add properties to entities
- Configure property types (string, int, DateTime, Guid, etc.)
- Set nullable/required flags
- Add data annotations:
  - [Key]
  - [Required]
  - [MaxLength(n)]
  - [EmailAddress]

#### 3. Relationship Management
- Add relationships between entities
- Support for:
  - **One-to-One**
  - **One-to-Many**
  - **Many-to-Many**
- Configure foreign keys
- Define navigation properties
- Set join tables (for many-to-many)

#### 4. Tabs
- **Properties Tab**: View and manage all properties
- **Relations Tab**: View and manage relationships
- **Indexes Tab**: Coming soon

### Example Workflow

1. Click "+" to create new entity (e.g., "Product")
2. Enter entity name and description
3. Add properties:
   - Name (string, Required, MaxLength 200)
   - Price (decimal)
   - CreatedAt (DateTime)
4. Add relation:
   - Type: One-to-Many
   - Target: Category
   - Foreign Key: CategoryId
5. View generated entity structure

---

## Navigation Structure

### Menu Items

**Dashboard Section:**
- Dashboard - Overview stats

**Query & Data Section:**
- Database Explorer - View database schema
- Query Builder (Basic) - Simple query builder
- Query Builder (Advanced) - Full-featured query builder with code gen
- Entity Manager - Manage entities and relationships

**Management Section:**
- Projects - Manage generated projects
- Users - User management

---

## API Integration

### Authentication Header

All API calls automatically include the JWT token:

```csharp
Http.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

### Services

- **CodeGenerationService**: Generates DTOs, Repositories, Services, Controllers
- **AuthStateProvider**: Manages authentication state
- **TokenProvider**: Stores JWT token

---

## Key Improvements

### Security
- JWT-based authentication
- All pages require login (except /login)
- Automatic token management
- Secure logout

### User Experience
- Professional login page
- Auth state in navigation bar
- User greeting ("Hello, username")
- Smooth redirects
- Loading indicators

### Code Generation
- Complete application layers
- Best practices implemented
- Copy-paste ready code
- Proper separation of concerns

### Entity Management
- Visual entity designer
- Relationship management
- Annotation support
- Like Amplication but in Blazor

---

## Next Steps

1. **Run the application**
   ```bash
   dotnet run --project ArchoCybo
   ```

2. **Login** with default credentials

3. **Try Query Builder**:
   - Select an entity
   - Build a query
   - Test it
   - Generate code

4. **Try Entity Manager**:
   - Create a new entity
   - Add properties
   - Add relationships

5. **Customize**:
   - Add more annotation types
   - Extend code generation templates
   - Add validation rules
   - Implement index management

---

## Technical Details

### Technologies Used
- Blazor Server (.NET 10)
- MudBlazor UI Framework
- JWT Authentication
- Entity Framework Core
- SignalR (for real-time updates)

### Architecture
- Clean Architecture
- Repository Pattern
- Unit of Work Pattern
- CQRS with MediatR
- Dependency Injection

### Code Generation
- Template-based generation
- Follows .NET conventions
- Includes best practices
- Production-ready code

---

## Troubleshooting

### Login Issues
- Check database connection
- Verify user exists in database
- Check JWT configuration in appsettings.json

### API Calls Failing
- Ensure you're logged in
- Check token expiration
- Verify API is running

### Code Generation Not Working
- Select an entity first
- Ensure entity has columns
- Check browser console for errors

---

## Support

For issues or questions:
1. Check browser console for errors
2. Check server logs
3. Verify database connectivity
4. Ensure all services are registered in Program.cs
