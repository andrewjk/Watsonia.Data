# Watsonia.Data — Future Work

This is the backlog for work that is deliberately **not** part of the modernization pass. Items are grouped by theme and roughly ordered by value. Each one states the current behaviour, the target, and what "done" means, so it can be picked up without re-doing the analysis.

Status: **backlog** — nothing here is committed to a release.

Related documents:
- `03 MODERNIZATION.md` — what changed and why
- `04 GENERATED PROXIES.md` — detailed design for the source-generator work
- `02 BENCHMARK.md` — baseline measurements

---

## 1. Query performance and caching

### 1.1 EF Core-style parameterized compiled-query cache (**highest priority**)

**Current behaviour:** `Database.CompiledQueryCache` is keyed by `CompiledQueryKeyBuilder`, which combines the expression shape **with the captured constant values**. A query such as `Customers.Where(c => c.CustomerID == id)` therefore creates a new cache entry for every `id`; the cache is capped at `MaxCompiledQueries = 512` and simply stops adding entries after that. Nested/grouped commands also substitute named values and re-run `StatementCreator.Compile` for every execution. There are no hit/miss metrics and no eviction policy.

**Target:** cache by **query shape + parameter types**, like EF Core's compiled-query cache:

- Build the key from the expression tree with captured values replaced by parameter slots: expression node structure, member/method identities, provider/config identity, include paths and parameter *types* — never the values.
- At execution, extract the values in slot order and invoke a cached `Func<object[], object>` (or a typed delegate) that binds them as ADO.NET parameters. SQL text must stay parameterized; no literal interpolation.
- Applies to top-level queries **and** the inner commands produced for grouped/nested queries, so a group key does not recompile a statement per row.
- Decide explicitly how collection parameters (`Contains(list)`) are handled: either non-cacheable, or expanded into a stable parameter list with a shape key that ignores element count (document the choice).
- Bound the cache with an LRU/size policy (configurable) instead of silently refusing new entries, and expose hit/miss/eviction/size metrics through the existing instrumentation.
- Preserve correctness for closure mutation, nullable values, culture-sensitive formatting and include paths.

**Done when:** a plan-cache test proves that two executions with different values share one entry and bind the new values correctly; a cache test proves eviction/bounds and concurrency safety; benchmarks show no allocation/throughput regression for repeated parameterized queries. `CompiledQueryKeyBuilder` and `Database.MaxCompiledQueries` are replaced by the new cache, and the roadmap/docs are updated.

### 1.2 Query plan/cache diagnostics

- Expose cache size, hit rate, misses and evictions (ActivitySource tags or counters).
- Log the reason a shape is not cacheable (anonymous method call, mutable closure, unsupported expression) at debug level.
- Add a test/benchmark switch that reports cacheability for a query.

### 1.3 Allocation reduction (`ObjectPool`/`ArrayPool`)

The benchmark data shows Watsonia LINQ queries allocate roughly 20–50× raw ADO.NET. Before pooling, profile the read path; likely candidates:

- parameter value arrays and `object[]` allocations per command
- `DbFieldReader`/alias lookup structures allocated per row or per command
- expression/statement rebuilding for inner commands (see 1.1)
- compiled-command delegate closures

Then evaluate `ArrayPool<T>`/`Microsoft.Extensions.ObjectPool` for command/parameter buffers and readers, measured with `MemoryDiagnoser`. Do not add pooling without benchmark evidence.

### 1.4 First-class logging/tracing hooks

`Watsonia.Data.Extensions` currently adapts the database's existing `OnBeforeExecuteCommand`/`OnAfterExecuteCommand` events; the event path allocates per command. Move to first-class instrumentation hooks in the core (interfaces or a lightweight observer) so the extension package does not pay delegate/allocation costs. Keep the event API for compatibility.

Include:

- parameter value redaction/sensitive-data policy for `db.statement`
- OpenTelemetry semantic-convention tags and duration/row-count metrics
- scope for transaction/connection boundaries

### 1.5 HybridCache / distributed cache

`MemoryCacheStore` (backed by `IMemoryCache`) is the only cache implementation. Add:

- `HybridCache` support on net8+ (keep the netstandard2.0 path on `IMemoryCache`)
- a distributed `ICacheStore` adapter and documented invalidation/tagging strategy
- cache stampede protection and per-entity cache policies
- explicit invalidation on update/delete (today this is a broad refresh)

---

## 2. Query functionality parity

### 2.1 Navigation-based joins in the fluent API

The fluent builder can express every join the LINQ engine emits, but only with explicit keys:

```csharp
Select.From(orderTable)
    .Join(customerTable, Col("o", "CustomerID"), Col("c", "CustomerID"))
```

The LINQ side lets users write `o.Customer.City` and joins implicitly through the mapper. Add `JoinRelated(o => o.Customer, "c")` (and a left-join variant) that resolves FK/PK columns through `DatabaseMapper`, so switching from LINQ to fluent does not require knowing the schema.

### 2.2 Execution semantics that SQL cannot express

The fluent builder now generates `LIMIT`/`TOP`, reversed ordering, `EXISTS` and scalar `CASE` SQL, but it is a SQL builder, not an executor. Document (and, if an execution layer is ever added, implement):

- `Single`/`SingleOrDefault` cardinality checks (the builder currently emits a plain select)
- `First`/`Last` default-value semantics (SQL returns no row; the caller supplies the default)
- top-level `Any`/`All`/`Contains`: SQL returns `1`/`0`, while LINQ returns `bool`
- nested collection and entity-graph materialization (client-side in LINQ, absent from the SQL builder)

### 2.3 Grouping API shape

The SQL capability for key/element/result selectors exists via explicit columns and chained aggregates. Add API-level parity if users need it:

- `GroupBy(key, elementSelector)`
- `GroupBy(key, (key, group) => projection)`
- aggregate overloads that default to the element selector

### 2.4 Upstream IQToolkit features not ported

The restored engine deliberately omits these; add only if a consumer needs them:

- provider `EntityPolicy` / deferred includes (`EntityPolicy`-based include tests were not ported; `Include()` on the query is covered)
- XML mapping
- interface-mapped element types
- any remaining provider-specific include-loading conventions

### 2.5 Remaining LINQ operators

The enabled test suites cover the operations that existed in the upstream tests (select/joins/grouping/distinct/paging/aggregates/any/all/contains). Audit and test the rest explicitly:

- `Union`, `Concat`, `Except`, `Intersect`
- `ElementAt`/`ElementAtOrDefault`, `SkipWhile`/`TakeWhile`
- `Cast`/`OfType`, `AsEnumerable`
- `SequenceEqual`, `DefaultIfEmpty` outside group joins
- `GroupJoin` projections beyond the covered shapes

Add translation + execution tests for whichever operators are supported, and throw clear `NotSupportedException`s for the rest.

### 2.6 Async follow-ups

Completed: end-to-end cancellation, streaming entity queries, explicit async loaders and async includes.

Remaining discussions:

- non-entity projections still materialize because the projector runs client-side
- `ValueTask`/`IAsyncDisposable` were deliberately deferred (no synchronously-completing hot paths; `Database` holds no unmanaged state). Revisit only if profiling changes that.
- raw-SQL `params` overloads remain tokenless for source compatibility

---

## 3. Source generators and NativeAOT

Follow `04 GENERATED PROXIES.md`:

- shape-only Roslyn generator with `[WatsoniaEntity]` opt-in; shape at compile time, naming/policy at runtime
- `[ModuleInitializer]` proxy registry and removal of per-entity `CreateType()` startup cost
- retain `Reflection.Emit` fallback for uncovered types until parity is proven
- add an AOT/trimming smoke-test app to CI (`PublishAot`, trim warnings as errors) and run the query/CUD suites there
- generate debugger-friendly sources, verify with compiler-verified C# instead of emitted IL
- remove `Watsonia.Data.Reference` once the generator output is authoritative
- verify `Microsoft.Data.SqlClient`/`Microsoft.Data.Sqlite` under NativeAOT for the supported scenarios

---

## 4. Migrations and schema management

`UpdateDatabase()` adds missing tables/columns but never drops, renames or versions. Build a real (small) migration story:

- snapshot/diff model (schema state stored in code or a file)
- explicit drop/rename operations with confirmation
- ordered, idempotent script files and a transaction per migration
- `dotnet` tool or CLI verbs: `migrations add`, `script`, `apply`, `status`
- provider-specific DDL (SQLite table rebuild vs SQL Server `ALTER`)
- test coverage for upgrade/downgrade, empty databases and enums (the current tests cover script generation and the empty-DB enum regression)

---

## 5. Providers and type system

### 5.1 SQL Server execution coverage in CI

SQL Server tests are opt-in via `WATSONIA_SQLSERVER_CONNECTION` and are skipped in CI. Add an Ubuntu job with the SQL Server Linux container and run the integration suite against it.

### 5.2 SQLite function parity

- Math functions are not supported in SQLite; 11 language tests skip SQLite by design. Decide whether to map `ABS`/trig/log/power or document the gap.
- `DateTime.DayOfWeek`/`DayOfYear` mappings return numbers; verify materialization semantics against the enum-typed LINQ surface.
- Decimal storage: the SQLite fixture stores `DECIMAL` values as REAL and parameters are now bound as doubles. Document the precision contract (or switch to TEXT + casts with a compatibility story) and test rounding/overflow.

### 5.3 Type mapping matrix

Add round-trip tests for enums, `Guid`, `DateTimeOffset`, `TimeSpan`, `byte[]`, `char`, nullable variants and provider-specific defaults. Include schema-update and query parameter paths.

### 5.4 Bulk/batch operations

- SQLite does not implement `DbBatch`; multi-statement command text is used. Revisit if a future provider version supports batches.
- Evaluate bulk insert (not just bulk update/delete) and document transaction participation.
- Add tests for `QueryFunction<T>`/table-valued functions and stored-procedure paths; they currently have no coverage.
- Concurrency: bulk update/delete and cascade save contain TODOs about transaction scope. Add optimistic concurrency/row-version support or document last-write-wins and the transactional guarantees explicitly.

---

## 6. Tests, CI and benchmarks

- **Execution tests for QueryBuilder:** its 244 tests assert SQL text only. Add a small execution suite that runs representative generated SQL against the fixtures and compares results with the LINQ engine.
- **Concurrency/stress:** parallel reads/writes, cache and dynamic-proxy reuse, cancellation under load.
- **Property/fuzz tests:** random expression trees → translation → execution equivalence on SQLite.
- **Coverage reporting** in CI (currently no coverage collection).
- **Nightly benchmarks** with regression thresholds (`BenchmarkDotNet` is adopted, but results are not tracked or gated). Keep the `verify` command as the correctness gate.
- **Provider matrix:** SQLite/macOS/Linux/Windows is covered; add SQL Server as above.
- **Failure diagnostics:** improve unsupported-expression errors with the offending node type/member and a hint.

---

## 7. Packaging, docs and release hygiene

- **Dependency currency:** top-level packages are current (`Microsoft.Extensions.*` 10.0.14 — the `GHSA-qj66-m88j-hmgj` advisory is cleared — MSTest 4.5.1, Dapper 2.1.89, EF Core/SqlClient/SQLite/BenchmarkDotNet latest). Keep them current, but do not force upstream transitive pins (SqlClient's `Microsoft.Extensions.*` 8.x, SQLitePCLRaw 2.x) without provider testing. Add a dependency-audit gate to CI (`dotnet list package --vulnerable --include-transitive` or NuGetAudit warnings-as-errors).
- Consider central package management (`Directory.Packages.props`) to avoid version drift.
- Refresh `README.md` and `01 SUMMARY.md`: they still describe Remotion.Linq-era query styles and "very beta" status. Document the IQToolkit-derived engine, async, fluent/LINQ parity, AOT/source-generator direction and the migration story.
- Add XML API docs/samples for the new fluent helpers (`WhereExists`, `WhereIn`, `ColumnSubquery`, `First/Last/Reverse`, scalar `Any/None/Contains`).
- NuGet/package hardening: symbols, SourceLink, deterministic builds, API-compatibility checks, license/readme metadata, and a release workflow.
- Document the deliberate design boundary: `Watsonia.Data.Query` **executes** queries; `Watsonia.QueryBuilder` **constructs SQL**. They intentionally share the same query surface, not execution semantics.

---

## 8. Deliberately not doing (unless requirements change)

- matching EF Core feature-for-feature (full graph change tracking, migrations parity, compiled models)
- nested collection/graph materialization in `QueryBuilder` (it is a SQL builder)
- `ValueTask`/`IAsyncDisposable` without a measured hot path
- a second full LINQ translator inside `QueryBuilder`; the fluent API is the supported alternative
- connection pooling inside the library (the ADO.NET providers already pool)
