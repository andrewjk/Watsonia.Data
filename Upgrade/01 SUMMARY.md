# Watsonia.Data — Repository Summary

## What it is
**Watsonia.Data** is a lightweight, open-source (MIT) **object-relational mapper (ORM) for .NET**, written in C# by Watsonia Publishing. It's designed to be dropped into a project and map entity classes to a database with minimal configuration. The README describes it as "very, very beta" — built for the author's own needs. Development appears dormant: the last commit was **April 2021**.

## Core features
- **Schema generation** — create/update database schema from entity classes (`UpdateDatabase()` / `GetUpdateScript()`)
- **Change-tracked entities** via dynamically generated **proxy classes** that override virtual properties (built with `System.Reflection.Emit`)
- **Three query styles**: LINQ (via `Remotion.Linq`), a fluent SQL API (`Select.From<Author>()...`), or raw SQL strings
- **Lazy loading** of related items/collections, with eager loading via `Include`
- **Bulk update/delete**, transactions, and cascade saves
- **Validation** using `System.ComponentModel.DataAnnotations` attributes or `IValidatableObject` (recursive across related entities)
- **Proxy export** to a static assembly for environments without dynamic codegen
- **Async support** — `Database.Async.cs` with `FirstAsync`, `ToListAsync`, etc.

## Projects in the solution
| Project | Purpose |
|---|---|
| `Watsonia.Data` | Core ORM — `Database`, `DatabaseConfiguration`, dynamic proxy factory, LINQ query provider, mapping, validation |
| `Watsonia.Data.SqlServer` | SQL Server plugin (command builder, data access provider, database updater) |
| `Watsonia.Data.SQLite` | SQLite plugin |
| `Watsonia.Data.Tests` | Unit/integration tests (cache, data, database, dynamic proxy, queries, saving) |
| `Watsonia.Data.Reference` | Hand-written reference implementations (e.g., `AuthorProxy`, `BookValueBag`) showing what generated proxies look like |
| `Watsonia.Data.TestPerformance` | Benchmarking console app comparing against **Entity Framework** |
| `Watsonia.QueryBuilder` (+ `.SqlServer` / `.SQLite`) | **Sibling repo** (`../Watsonia.QueryBuilder`) providing the fluent SQL engine — referenced by relative path |

## Technical details
- **Targets**: `net5.0` and `netstandard2.0`
- **Key dependencies**: `Remotion.Linq` (LINQ provider), `System.Reflection.Emit` (proxies), `Microsoft.CSharp`
- **Architecture**: provider/plugin model (`IDataAccessProvider`) so other databases can be added; conventions (table/column naming, `ID` primary key, lazy-loading rules) are all overridable via a custom `DatabaseConfiguration`
- Signed assemblies (`Watsonia.snk`), with an `.editorconfig` for consistent style

In short: a small, self-contained ORM in the spirit of SubSonic/PetaPoco — simple conventions, proxy-based change tracking, and swappable database backends — that was last actively worked on in early 2021.
