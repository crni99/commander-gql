# 🚀 CommanderGQL — Upgrade Notes (.NET 5 → .NET 8)

This project was built following the tutorial
**[GraphQL API with .NET 5 and Hot Chocolate](https://www.youtube.com/watch?v=HuN94qNwQmM)** by **Les Jackson**.

The original tutorial targets **.NET 5** and **Hot Chocolate v11/v12**. To run the project on **.NET 8** with **Hot Chocolate v13+**, several changes were required. This document lists them side by side.

## 📑 Table of Contents

- [🚀 CommanderGQL — Upgrade Notes (.NET 5 → .NET 8)](#-commandergql--upgrade-notes-net-5--net-8)
  - [📑 Table of Contents](#-table-of-contents)
  - [1. Project Setup \& CLI Tools](#1-project-setup--cli-tools)
  - [2. DbContext Registration (`Program.cs`)](#2-dbcontext-registration-programcs)
    - [The problem](#the-problem)
    - [The solution](#the-solution)
  - [3. Query Resolvers \& Attributes (`Query.cs`)](#3-query-resolvers--attributes-querycs)
  - [4. Type Resolvers (`CommandType.cs` \& `PlatformType.cs`)](#4-type-resolvers-commandtypecs--platformtypecs)
    - [`CommandType.cs`](#commandtypecs)
    - [`[Parent]` in nested resolvers](#parent-in-nested-resolvers)
  - [5. GraphQL Voyager](#5-graphql-voyager)

---

## 1. Project Setup & CLI Tools

`dotnet-ef` is **not** bundled with the SDK (neither in .NET 5 nor in .NET 8). Install it as a global tool:

```bash
dotnet tool install --global dotnet-ef

# or update an existing installation
dotnet tool update --global dotnet-ef
```

Migrations also require the design-time package in the startup project:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

---

## 2. DbContext Registration (`Program.cs`)

### The problem

Sending a query with multiple aliases for the same field, e.g.

```graphql
query {
  a: platform { name }
  b: platform { name }
}
```

makes Hot Chocolate run the resolvers **in parallel**. If they share a single scoped `DbContext`, EF Core throws:

> `A second operation was started on this context instance before a previous operation completed.`

### The solution

Use a **pooled `DbContext` factory** and register it with Hot Chocolate, so every resolver gets its own context from the pool.

**Original (.NET 5 / HC v11–v12)**

```csharp
services.AddPooledDbContextFactory<AppDbContext>(opt =>
    opt.UseSqlServer(Configuration.GetConnectionString("CommandConStr")));

services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<PlatformType>()
    .AddType<CommandType>()
    .AddFiltering()
    .AddSorting();
```

**Updated (.NET 8 / HC v13+)**

```csharp
builder.Services.AddPooledDbContextFactory<AppDbContext>(opt =>
    opt.UseSqlServer(connectionString));

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .RegisterDbContext<AppDbContext>(DbContextKind.Pooled) // Hot Chocolate EF Core integration
    .AddProjections()
    .AddFiltering()
    .AddSorting();
```

> [!NOTE]
> `RegisterDbContext<T>(DbContextKind.Pooled)` comes from the `HotChocolate.Data.EntityFramework` package.

---

## 3. Query Resolvers & Attributes (`Query.cs`)

In Hot Chocolate v13+, `[ScopedService]` is deprecated. With `RegisterDbContext<AppDbContext>(DbContextKind.Pooled)` in `Program.cs`, the `DbContext` can be injected directly into resolvers and its lifecycle is managed by Hot Chocolate, so `[UseDbContext]` is no longer needed.

**Original (.NET 5 / HC v11–v12)**

```csharp
[UseDbContext(typeof(AppDbContext))]
[UseFiltering]
public IQueryable<Platform> GetPlatform([ScopedService] AppDbContext context)
{
    return context.Platforms;
}
```

**Updated (.NET 8 / HC v13+)**

```csharp
[UseProjection]
[UseFiltering]
[UseSorting]
public IQueryable<Platform> GetPlatform(AppDbContext context)
{
    return context.Platforms;
}
```

> [!IMPORTANT]
> Attribute order matters. Always use: `[UseProjection]` → `[UseFiltering]` → `[UseSorting]`.

---

## 4. Type Resolvers (`CommandType.cs` & `PlatformType.cs`)

When resolving relationships with `ResolveWith<T>()`, the `.UseDbContext<AppDbContext>()` call on the field descriptor is no longer needed.

### `CommandType.cs`

**Original (.NET 5)**

```csharp
descriptor
    .Field(c => c.Platform)
    .ResolveWith<Resolvers>(c => c.GetPlatform(default!, default!))
    .UseDbContext<AppDbContext>() // no longer needed
    .Description("This is the platform to which the command belongs.");
```

**Updated (.NET 8)**

```csharp
descriptor
    .Field(c => c.Platform)
    .ResolveWith<Resolvers>(c => c.GetPlatform(default!, default!))
    .Description("This is the platform to which the command belongs.");
```

### `[Parent]` in nested resolvers

Always mark the parent entity parameter with `[Parent]`. Without it, Hot Chocolate treats the parameter as a **required GraphQL argument** (e.g. `PlatformInput!`) that the client would have to send.

```csharp
private class Resolvers
{
    public IQueryable<Command> GetCommands([Parent] Platform platform, AppDbContext context)
    {
        return context.Commands.Where(c => c.PlatformId == platform.Id);
    }
}
```

---

## 5. GraphQL Voyager

The old `GraphQLVoyagerOptions` type was replaced by `VoyagerOptions` in newer versions of `GraphQL.Server.Ui.Voyager`.

**Original (.NET 5)**

```csharp
app.UseGraphQLVoyager(new GraphQLVoyagerOptions()
{
    GraphQLEndPoint = "/graphql",
    Path = "/graphql-voyager"
});
```

**Updated (.NET 8)**

```csharp
using GraphQL.Server.Ui.Voyager;

app.MapGraphQL(); // maps the Hot Chocolate endpoint at /graphql

app.UseGraphQLVoyager("/graphql-voyager", new VoyagerOptions
{
    GraphQLEndPoint = "/graphql"
});
```

> [!WARNING]
> The Voyager registration API differs between package versions. Check the exact method signature for the version you installed.
