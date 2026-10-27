## Benchmark results

The `Watsonia.Data.TestPerformance` project had no committed results (it writes `Data\Log *.txt` at runtime), so I got it running on this machine. The scenario and dataset shape are based on [exceptionnotfound's Dapper vs EF vs ADO.NET benchmark](https://www.exceptionnotfound.net/dapper-vs-entity-framework-vs-ado-net-performance-benchmarking/); the current BenchmarkDotNet implementation is a rewrite: 5 runs × 5 frameworks × 5 query scenarios against a SQLite database (500 posts, 5 sports, 50 teams, 500 players), averaging milliseconds per operation. All frameworks were cross-checked to return **identical data** (no correctness warnings).

| Framework | Post IDs | Posts | Player by ID | Players / Team | Teams / Sport |
|---|---|---|---|---|---|
| **AdoNet** (baseline) | 0.22 ms | 2.12 ms | 0.012 ms | 0.048 ms | 0.08 ms |
| **Dapper** | 0.16 ms (73%) | 2.54 ms (120%) | 0.048 ms (400%) | 0.00 ms (0%) | 0.12 ms (150%) |
| **EntityFramework** | 2.41 ms (1077%) | 3.02 ms (143%) | 0.112 ms (933%) | 0.036 ms (75%) | 1.36 ms (1700%) |
| **Watsonia LINQ** | 0.49 ms (220%) | 3.88 ms (183%) | 0.040 ms (333%) | 0.124 ms (258%) | 0.84 ms (1050%) |
| **Watsonia SQL** | 0.08 ms (34%) | 2.86 ms (135%) | 0.060 ms (500%) | 0.012 ms (25%) | 0.00 ms (0%) |

**Takeaways:** raw SQL beats everything (Watsonia's fluent-SQL mode is essentially at ADO.NET speed). Watsonia's LINQ mode is competitive with Dapper for simple queries, and **dramatically faster than EF Core** on ID-list and paged/collection queries (~4–15×), though single-entity loads carry more overhead than Dapper. EF pays heavily for its query pipeline on SQLite.

## What it took to run it (2021 codebase on a 2026 arm64 Mac)

The benchmark couldn't run as-is; I fixed four compatibility issues:

1. **`Config.cs`** — Windows-only `Data\Performance.sqlite` path → `/`
2. **Test csproj** — bumped `SQLitePCLRaw.bundle_e_sqlite3` to 2.1.11 (2.0.4 predates Apple Silicon, no arm64 `e_sqlite3`)
3. **`DynamicProxyFactory.cs`** — the generated proxy IL pushed a `Nullable<long>` into non-nullable `long` FK setters/getters, which old JITs tolerated but modern ones reject (`InvalidProgramException`). It now emits against the FK property's actual declared type
4. **`QueryableMethods.cs`** — .NET 6+ added `ElementAt(Index)`/`Take(Range)` overloads, breaking the method lookups; now also matches parameter types (same fix EF Core shipped in its 5.0 patches)

I also made the unit-test suite runnable (`Data\` → `Data/` paths, an assembly-init shim for working directory): **638/642 tests pass**, including all dynamic-proxy and saving suites. The 4 remaining failures are pre-existing quirks — newer SQLite native lib behavior on decimal/date comparisons, and baseline drift from the sibling QueryBuilder repo being a month newer. Worth noting this means the proxy fix I made is validated by both the benchmark's correctness cross-check and ~640 unit tests.

---

# BenchmarkDotNet results (full run, October 2026)

The hand-rolled harness was replaced with BenchmarkDotNet (`Feat: adopt BenchmarkDotNet`). Full DefaultJob run on this machine — 25 benchmarks, ~8.5 minutes:

- BenchmarkDotNet 0.15.8, Apple M2 (8 cores), macOS, .NET 10.0.2 arm64, Concurrent Workstation GC
- Database: SQLite, 500 posts / 5 sports / 50 teams / 500 players; each benchmark class seeds in `[GlobalSetup]`
- Data correctness was verified separately with `dotnet run -- verify` (all frameworks returned identical data)

## Time (mean, µs) and ratio vs raw ADO.NET

| Scenario | ADO.NET | Dapper | EF Core | Watsonia SQL | Watsonia LINQ |
|---|---|---|---|---|---|
| Post IDs | 331.5 | 362.6 (1.09×) | 390.7 (1.18×) | 359.0 (1.08×) | 620.7 (1.87×) |
| Posts | 859.7 | 922.6 (1.07×) | 1,212.7 (1.41×) | 1,087.1 (1.26×) | 3,690.6 (4.29×) |
| Player by ID | 7.96 | 9.08 (1.14×) | 49.72 (6.25×) | 9.54 (1.20×) | 9.36 (1.18×) |
| Players / team | 23.92 | 26.92 (1.13×) | 62.42 (2.61×) | 29.58 (1.24×) | 671.4 (28.1×) |
| Teams / sport | 90.40 | 132.1 (1.46×) | 391.3 (4.33×) | 147.9 (1.64×) | 2,717.7 (30.1×) |

## Allocations (per operation)

| Scenario | ADO.NET | Dapper | EF Core | Watsonia SQL | Watsonia LINQ |
|---|---|---|---|---|---|
| Post IDs | 9.3 KB | 33.5 KB | 162.5 KB | 33.2 KB | 60.0 KB |
| Posts | 2.06 MB | 2.11 MB | 2.49 MB | 2.59 MB | 2.64 MB |
| Player by ID | 1.7 KB | 2.3 KB | 66.7 KB | 8.5 KB | 8.5 KB |
| Players / team | 3.6 KB | 5.3 KB | 67.2 KB | 18.9 KB | 186.0 KB |
| Teams / sport | 21.9 KB | 46.5 KB | 171.4 KB | 135.0 KB | 443.0 KB |

## Observations

1. **Watsonia SQL is at parity with Dapper** (1.08–1.64× ADO.NET across all five scenarios) — the fluent API adds almost nothing over hand-written SQL.
2. **Watsonia LINQ is fine on single entities** (`GetPlayerByID` beats EF Core, 9.4µs vs 49.7µs, and is within 18% of ADO.NET) but **pathological on relation loading**: 28–30× slower than ADO.NET and 4–11× slower than EF Core on players/team and teams/sport, allocating 186–443 KB per call.
3. **This revises the earlier (hand-rolled harness) claim** that Watsonia LINQ was "dramatically faster than EF Core on collection queries". Rigorous measurement shows the opposite for relation loading; the old harness was warming caches across frameworks and averaging noise.
4. EF Core pays per-call overhead on single entities (6.25× ADO.NET, 67 KB) but is comparatively good on sets — its compiled query pipeline amortizes.

## Phase diagnostics (ShortRun, diagnostic benchmarks)

Splitting a LINQ query into translate → compile → execute showed where the overhead is:

| Phase | Players-by-team (10 rows) | Teams-include (10 teams, 100 players) |
|---|---|---|
| `GetExecutionPlan` (translate) | 205 µs | 211 µs |
| `Expression.Compile` of plan | 303 µs | 302 µs |
| Full query | 875 µs | 2,764 µs |

- **Translation + plan compilation ≈ 500µs per LINQ execution** — the provider re-translates and re-compiles the plan on every call (the code carries a `// TODO: Implement the cache...` for exactly this).
- For the include scenario, the remaining ~2.25ms is the include machinery (`LoadIncludePaths`/`SetChildItems`: nested parent×child scans plus per-parent reflection), which is why relation loading trails raw SQL by an order of magnitude.
- These two findings set the priorities for further optimization: query plan caching/parameterization and include-path loading.

## How to run

```bash
dotnet run -c Release --project Watsonia.Data.TestPerformance -- --filter "*"   # full suite
dotnet run -c Release --project Watsonia.Data.TestPerformance -- --filter "*WatsoniaLinq*"
dotnet run -c Release --project Watsonia.Data.TestPerformance -- verify         # data cross-check
```

Reports (Markdown/CSV/HTML) are written to `BenchmarkDotNet.Artifacts/results/` (gitignored).

---

# Optimization round 1 — LINQ plan caching and include loading (October 2026)

After the diagnostic benchmarks showed translation (≈205µs) and plan compilation (≈302µs) being paid on **every** LINQ execution, plus O(parents × children) work in include distribution, three changes were made:

1. **Per-database compiled query plan cache** (`CompiledQueryKeyBuilder` + `Query.Provider.Execute`): plans are cached keyed by expression shape *and* captured parameter values (including a query's Include paths), bounded to 512 entries. Unsafe shapes (non-LINQ method calls, mutable collections) are not cached.
2. **Include distribution rewritten as lookups** (`Database.SetChildItems`): children are grouped by foreign key/primary key once instead of scanning the child set per parent; collection instances come from cached compiled factories; `DynamicProxyStateTracker.SetCollection` caches parent-reference property discovery.
3. **Benchmark tests reuse one Database per framework** instead of constructing one per operation (what real applications do, and required for the plan cache to be exercised).

## Results (full DefaultJob, same machine, before → after)

| Scenario | ADO.NET | Dapper | EF Core | Watsonia SQL | Watsonia LINQ (before) | Watsonia LINQ (after) |
|---|---|---|---|---|---|---|
| Post IDs | 331.3 µs | 353.7 | 386.8 | 351.8 | 620.7 µs | **360.6 µs** (1.09× ADO) |
| Posts | 814.4 µs | 889.6 | 1,141.7 | 1,067.3 | 3,690.6 µs | **1,047.7 µs** (1.29×) |
| Player by ID | 7.7 µs | 8.7 | 48.2 | 9.0 | 9.4 µs | **9.0 µs** (1.16×) |
| Players / team | 23.8 µs | 26.8 | 61.8 | 28.9 | 671.4 µs | **35.2 µs** (1.48×) |
| Teams / sport | 87.2 µs | 132.8 | 383.5 | 145.5 | 2,717.7 µs | **175.8 µs** (2.02×) |

Allocations (Watsonia LINQ): Post IDs 60.0 → **23.0 KB**; Posts 2.64 → 2.60 MB; Player by ID 8.5 → **4.7 KB**; Players/team 186.0 → **23.5 KB**; Teams/sport 443.0 → **201.5 KB**.

Improvements: **1.7× / 3.5× / 19× / 15×** (post-IDs / posts / players-per-team / teams-per-sport). Watsonia LINQ now beats EF Core on every scenario and is at or near parity with the fluent-SQL path and Dapper; the remaining 2× gap on teams/sport is include loading plus proxy/value-bag allocation.

## Caveats and next steps

- The plan cache is keyed by **shape + captured values**, so it helps repeated identical queries (static filters, list screens, paging) but not high-cardinality parameters. EF-style shape-only caching needs true parameterization of captured values into the plan (a follow-up).
- Cached plans are per `Database` instance and bounded (512); the benchmarks' shared per-framework `Database` reflects typical application usage.
- The first execution of each distinct query shape still pays translate+compile (≈0.5ms).
- Remaining allocation hotspot is entity materialization/projection (≈2.6MB for 500 posts, dominated by 500 proxies/value bags) — the domain of the source-generated-proxy work.
- `ResearchBenchmarks` (translate/compile/full phase split) is part of the benchmark project for future diagnostics.

---

# Latest-dependency run — Dapper 2.1.89 (October 2026)

The benchmark project's dependencies were brought up to their current versions before this run: **Dapper 2.0.78 → 2.1.89** (the only benchmark-relevant change), with EF Core 10.0.14, Microsoft.Data.Sqlite 10.0.14, Microsoft.Data.SqlClient 7.1.1 and BenchmarkDotNet 0.15.8 already current. The same run also follows the `Microsoft.Extensions.*` 8.0.0 → 10.0.14 security update (not used by the benchmark) and MSTest 2.x → 4.5.1 (test-only).

- BenchmarkDotNet 0.15.8, Apple M2 (8 cores), macOS 27.0.1, .NET SDK 10.0.102 / runtime 10.0.2 arm64, DefaultJob
- 31 benchmarks, ~10 minutes; SQLite fixture with 500 posts / 5 sports / 50 teams / 500 players
- Data correctness re-verified with `dotnet run -c Release --project Watsonia.Data.TestPerformance -- verify` (all frameworks identical)

## Time (mean, µs) and ratio vs raw ADO.NET

| Scenario | ADO.NET | Dapper | EF Core | Watsonia SQL | Watsonia LINQ |
|---|---|---|---|---|---|
| Post IDs | 341.1 | 365.6 (1.07×) | 393.7 (1.15×) | 355.3 (1.04×) | 357.7 (1.05×) |
| Posts | 860.0 | 913.9 (1.06×) | 1,141.3 (1.33×) | 1,091.7 (1.27×) | 1,071.3 (1.25×) |
| Player by ID | 7.62 | 8.98 (1.18×) | 48.55 (6.37×) | 9.03 (1.19×) | 9.01 (1.18×) |
| Players / team | 23.79 | 26.80 (1.13×) | 62.93 (2.65×) | 29.48 (1.24×) | 34.77 (1.46×) |
| Teams / sport | 86.51 | 132.84 (1.54×) | 383.38 (4.43×) | 148.79 (1.72×) | 177.01 (2.05×) |

## Allocations (per operation)

| Scenario | ADO.NET | Dapper | EF Core | Watsonia SQL | Watsonia LINQ |
|---|---|---|---|---|---|
| Post IDs | 9.26 KB | 33.55 KB | 162.48 KB | 29.28 KB | 23.00 KB |
| Posts | 2.01 MB | 2.06 MB | 2.43 MB | 2.52 MB | 2.54 MB |
| Player by ID | 1.69 KB | 2.32 KB | 66.66 KB | 4.66 KB | 4.66 KB |
| Players / team | 3.54 KB | 5.35 KB | 67.23 KB | 15.04 KB | 23.47 KB |
| Teams / sport | 21.84 KB | 46.49 KB | 171.37 KB | 131.04 KB | 201.44 KB |

## Observations

1. **Dapper 2.1.89 is within noise of 2.0.78** on every scenario (e.g. 353.7 → 365.6 µs Post IDs, 889.6 → 913.9 µs Posts, 132.8 → 132.8 µs Teams/sport), so the previous comparison remains valid.
2. Watsonia's ordering is unchanged: fluent SQL is at ADO.NET/Dapper parity; LINQ is fastest on single entities (18% over ADO.NET, ~5.4× faster than EF Core) but pays its include/materialization cost on relation loads (2.05× ADO.NET for teams/sport).
3. There are no new outliers or correctness warnings; the benchmark's cross-framework `verify` check passes.
4. EF Core remains the slowest for single-entity loads (6.37× ADO.NET, 67 KB per call), but competitive on sets.
5. Top-level packages are now all on their latest versions. Transitive pins that come from upstream providers were intentionally **not** forced (for example SqlClient's `Microsoft.Extensions.*` 8.x and Microsoft.Data.Sqlite's SQLitePCLRaw 2.x) because lifting them independently is unsupported by those providers; they contain no current advisories.
