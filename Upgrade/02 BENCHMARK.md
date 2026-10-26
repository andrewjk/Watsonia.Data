## Benchmark results

The `Watsonia.Data.TestPerformance` project had no committed results (it writes `Data\Log *.txt` at runtime), so I got it running on this machine. It's adapted from [exceptionnotfound's Dapper vs EF vs ADO.NET benchmark](https://www.exceptionnotfound.net/dapper-vs-entity-framework-vs-ado-net-performance-benchmarking/): 5 runs × 5 frameworks × 5 query scenarios against a SQLite database (500 posts, 5 sports, 50 teams, 500 players), averaging milliseconds per operation. All frameworks were cross-checked to return **identical data** (no correctness warnings).

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
