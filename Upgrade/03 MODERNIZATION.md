# Watsonia.Data — Modernization Notes

Yes — several, and they're structural rather than cosmetic. Framed against what's actually in this repo:

## The fork in the road

First, the macro question the library needs answered: **what is its niche in 2026?** In 2021 the pitch was "simpler than EF, tracked entities, minimal fuss." Today EF Core 10 and Dapper.AOT have absorbed huge ground. The credible modern positioning for something Watsonia-shaped is: *convention-based micro-ORM with tracked entities that works under NativeAOT* — because ironically, the features built for "environments with no dynamic support" (the `ExportProxies` feature!) are exactly what's first-class now, while the implementation makes the library *unable to run there*. Everything below follows from that.

## Big rocks, in priority order

### 1. Replace Reflection.Emit with source generators
The single biggest change. `DynamicProxyFactory.cs` (2,400 lines of IL emission), `IDynamicProxy`, the `ExportProxies` escape hatch, and even the `Watsonia.Data.Reference` project all exist to work around one fact: entity tracking needed runtime codegen. Roslyn source generators (plus C# 12 interceptors, the mechanism Dapper.AOT is built on) solve this at compile time:

- **NativeAOT/trimming compatible** — Reflection.Emit is flat-out banned under NativeAOT; the library can't run there at all today
- **Deletes the bug class we just fixed** — the `InvalidProgramException` was emitted-at-runtime IL; generated C# is compiler-verified and debuggable
- **No startup cost** — no `CreateType()` warm-up per entity
- The `Reference` project is literally a hand-written spec of what the generator should output — it's half the design work already done

This is where the ecosystem went: Dapper.AOT, Mapperly, EF Core compiled models.

### 2. Get off Remotion.Linq (a dead dependency)
Last release 2.2.0 in **2018**; EF Core abandoned it in 3.0 and wrote its own pipeline; effectively nothing maintains it. This is the riskiest dependency in the library. Options, in increasing effort:
- **De-emphasize LINQ**: your benchmark shows the fluent SQL API is already the fastest path (roughly ADO.NET speed) — make it the headline API and keep LINQ as a legacy/compat surface
- **Own the pipeline**: hand-rolled expression visitor (what EF Core did) — a serious multi-month investment
- Interim: keep it on `netstandard2.0` where it still works, but don't build new features on it

### 3. Driver hygiene (cheap, do first)
- `System.Data.SqlClient` → **`Microsoft.Data.SqlClient`** — the current package is already flagged vulnerable by NuGet audit (the `NU1903` warning in our build output)
- **`DbBatch`** (.NET 7+) — first-class ADO.NET batching, supported by both SqlClient and Microsoft.Data.Sqlite; replaces hand-rolled bulk DML
- Drop the `net5.0` target (EOL); target `netstandard2.0` + current LTS (net8/net10)

### 4. Async as designed-in, not bolted-on
The 2021 async (`Database.Async.cs`) mirrors sync signatures. Modern shape: `IAsyncEnumerable<T>` with `await foreach` **streaming** from `DbDataReader` (never materialize whole result sets), `CancellationToken` on every operation, `ValueTask`, `IAsyncDisposable`.

### 5. Ecosystem integration (the "modern library" expectations)
A data-access library in 2026 is expected to plug into the Microsoft.Extensions stack:
- **`ActivitySource`** → OpenTelemetry DB tracing
- **`Microsoft.Extensions.Logging`** abstractions instead of (or alongside) event handlers
- **`IServiceCollection`** registration + **Options pattern** for `DatabaseConfiguration`
- **`HybridCache`** (.NET 9) or `IMemoryCache` to replace the hand-rolled `ItemCache`
- `ObjectPool`/`ArrayPool` for connection and SQL-text reuse

### 6. Smaller-but-real items
- **BenchmarkDotNet** to replace the hand-rolled stopwatch harness (no warmup/statistics discipline) — keep the cross-framework comparison, get credible numbers
- **Migration story**: `UpdateDatabase()` never drops/renames and has no snapshots; even a script-file migration model would clear the biggest functional gap vs EF
- Test fixtures: committed binary `.sqlite` files → generated fixtures; CI matrix covering Linux/macOS (this week proved the suite was Windows-only in practice)

## What *not* to chase
Don't try to match EF Core feature-for-feature (multi-table mapping, full change-tracking graphs). That's a Microsoft-sized investment. The defensible version of this library is smaller, faster, AOT-clean, and conventional.
