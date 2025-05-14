
# EF Core Migrations Guide

## What is EF Core Migration?

Entity Framework Core (EF Core) is a powerful and popular Object-Relational Mapper (ORM) for .NET, including C#. It allows developers to:

- Define data models as C# classes.
- Map these models to relational database tables.
- Interact with the database using C# code instead of writing raw SQL.

### What Are Migrations?

EF Core Migrations are a feature that enables you to evolve your database schema over time in a structured and maintainable way as your data models change. Here's how the workflow typically looks:

1. Define your data models in C#.
2. Use EF Core to generate migration files that describe the necessary schema changes.
3. Apply these migrations to update your actual database schema.

## Why Use EF Core Migrations?

- **Schema Evolution**: Easily manage and apply incremental schema changes as your application grows.
- **Version Control**: Migrations are code files, which means they can be committed to source control (e.g., Git) to track changes over time.
- **Consistency**: Keeps your C# models and your database schema in sync, minimizing bugs and mismatches.

## PostgreSQL Support

If you're using PostgreSQL, the most widely used EF Core provider is:

- `Npgsql.EntityFrameworkCore.PostgreSQL`

## Required Packages

To use EF Core Migrations with PostgreSQL, you need to install the following NuGet packages:

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

### Explanation:

- `Microsoft.EntityFrameworkCore`: Core EF functionality.
- `Microsoft.EntityFrameworkCore.Design`: Required for migrations and tooling support.
- `Npgsql.EntityFrameworkCore.PostgreSQL`: PostgreSQL database provider for EF Core.
### Configure Connection String

Configure the connection string in `appsettings.json` to match your Postgres service:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=postgres;Port=5432;Database=bitandbeam;Username=postgres;Password=postgres"
}
```

**Note:** This connection string works because, in Docker Compose, service names (like `postgres`) are used as hostnames for networking between containers.

## Translating the SQL Schema into C# Models

To integrate your PostgreSQL schema with EF Core, the following steps were taken to translate SQL tables into C# classes within the `Models/` directory:

- Analyzed comprehensive PostgreSQL schema, including tables like `Users`, `Buildings`, `Documents`, `DocumentTags`, and `DocumentTagRelation`.
- For each table, a corresponding C# class was created (e.g., `User`, `Building`, `Document`, `DocumentTag`, `DocumentTagRelation`).
- Each class included properties that map directly to the columns in the SQL table. For example, the `DocumentTag` class look like this:

## Example: `DocumentTag` Model

The following is an example of how a PostgreSQL table (`DocumentTag`) is translated into a C# class in EF Core. This model maps directly to a database table and includes relationships via navigation properties.

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BUILD.ING.Models
{
    public class DocumentTag
    {
        [Key]
        public int TagId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<DocumentTagRelation> DocumentTagRelations { get; set; }
    }
}
```
### Configuring the DbContext

Created `AppDbContext` in the `Data/` directory.
Added `DbSet<T>` properties for each entity (e.g., `public DbSet<DocumentTag> DocumentTags { get; set; }`).

Registered the DbContext in `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

Configured relationships and constraints using navigation properties and the Fluent API in `OnModelCreating` (e.g., for many-to-many relations between `Document` and `DocumentTag` via `DocumentTagRelation`).

#### Example: Relationship Configuration in Fluent API

```csharp
modelBuilder.Entity<DocumentTagRelation>()
    .HasKey(dtr => new { dtr.DocumentId, dtr.TagId });
modelBuilder.Entity<DocumentTagRelation>()
    .HasOne(dtr => dtr.Document)
    .WithMany(d => d.DocumentTagRelations)
    .HasForeignKey(dtr => dtr.DocumentId);
modelBuilder.Entity<DocumentTagRelation>()
    .HasOne(dtr => dtr.Tag)
    .WithMany(t => t.DocumentTagRelations)
    .HasForeignKey(dtr => dtr.TagId);
```

## Migrations

To create and apply migrations, run the following commands:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Inspecting the Database with a Client

* Installed DBeaver.
* Started Docker containers (with `docker-compose up`).
* Connected to the running PostgreSQL database using:
	+ Host: `localhost`
	+ Port: `5432`
	+ Database: `bitandbeam`
	+ User: `postgres`
	+ Password: `postgres`

### Example: Testing Connection in DBeaver

![Test Connection dialog in DBeaver](images/test-connection.png)

_A successful connection test in DBeaver._

### Example: DBeaver Interface with Database Tables

![DBeaver main interface showing tables](images/dbeaver-overview.png)

_The DBeaver interface showing the connected database and tables._

---

### Validation Checklist

- [ ] All expected tables (Users, Buildings, Documents, DocumentTags, etc.) appear in DBeaver.
- [ ] Table columns and types match your C# models and ER diagram.
- [ ] Relationships (foreign keys, many-to-many join tables) are present.
- [ ] Unique constraints and indexes are created as required.
- [ ] No errors during migration or Docker container startup.