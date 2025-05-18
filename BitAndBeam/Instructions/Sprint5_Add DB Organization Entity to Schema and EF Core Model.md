# Sprint 5: Add Organization Entity to Database Schema and EF Core Model

## Objective
Add a new `Organization` entity to the BUILD.ING Document Management System, establish relationships to existing models, and update all relevant documentation and diagrams.

---

## Steps

### 1. Add Organization Entity to the Data Model
- Create a new C# class `Organization` in `Models/Organization.cs` with fields:
  - `OrganizationId` (PK, int)
  - `Name` (string, required, unique)
  - `Description` (string, optional)
  - `CreatedAt` (DateTime, default: now)
  - `IsActive` (bool, default: true)
- Add navigation properties:
  - `ICollection<User> Users`
  - `ICollection<Building> Buildings`

### 2. Update User and Building Models
- Add a required `OrganizationId` foreign key and navigation property to both `User` and `Building` models.
- Update their constructors or property initializations as needed.

### 3. Update the DbContext
- Add `DbSet<Organization> Organizations` to `AppDbContext`.
- In `OnModelCreating`, configure:
  - Primary key, unique constraint on `Name`, required fields, and default values.
  - Relationships: `User.OrganizationId` and `Building.OrganizationId` as required FKs.

### 4. Create and Apply EF Core Migrations
- Run:
  ```sh
  dotnet ef migrations add AddOrganizationEntity
  dotnet ef migrations add RequireUserAndBuildingOrganization
  dotnet ef database update
  ```
- Ensure the database is up to date and all constraints are enforced.

### 5. Update Documentation
- Update backend `README.md` to describe the Organization entity and its relationships.
- Update the DBML file (`database/database_diagram.dbml`) to include the `organizations` table and new relationships.
- Ensure database documentation references the updated diagram and model.

### 6. Verify Criteria
- [x] Organization entity exists in the schema and code.
- [x] Users and Buildings are linked to an Organization (required FK).
- [x] DbContext and migrations are up to date.
- [x] Documentation and diagrams are updated.

---

## Notes
- All users and buildings must now belong to an organization.
- This enables multi-tenancy and organization-based data access control.
- Further steps may include updating controllers/services to enforce organization-based access in API logic.

---

**For any issues, check the migration history and ensure the database matches the EF Core model.**
