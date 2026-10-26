# Generated Proxies — Design Notes

Status: **design phase** — the `origin/generator` branch was merged experimentally and reverted (revert commit `d23293a`). This doc captures why, and the design for the real implementation.

## Goal

Replace `System.Reflection.Emit` (`DynamicProxyFactory.cs`, ~2,400 lines of runtime IL) with compile-time generated proxies, so that:

- Generated entities work under **NativeAOT / trimming** (Reflection.Emit is banned there)
- Generated proxies are **debuggable, compiler-verified C#** (deletes the emitted-IL bug class we hit on net10)
- No proxy-generation startup cost

## Why the abandoned branch was abandoned (validated)

`origin/generator` is a **design-time console tool** (`dataconfig.json` regex rules → writes `XProxy.cs`/`XValueBag.cs` into the project). Its fatal flaw: it tried to **bake resolved schema values** into the proxies (`__TableName = "Player"`, `__ColumnMappings`) derived from JSON. That cannot express the ~30 virtual customization points on `DatabaseConfiguration` (`GetTableName` pluralization, `GetForeignKeyColumnName` SportsID hack, `ShouldMapType/Property`, `ShouldCascade`, `IsTable/IsView/...`).

Merging it experimentally reproduced exactly those failures: baked `"Player"` fought the runtime pluralization override; `Player` + `PlayerProxy` both mapped to schema table "Players" (duplicate key); the bench DB grew junk `PostProxys`/`PlayerProxys` tables and the generated framework silently queried empty shadow tables.

## The key insight: shape vs naming

The Emit factory already splits the world at runtime — generated code must preserve that split:

| | Emit today | Generated proxies must |
|---|---|---|
| **Shape** — which properties exist; which are related items / collections / FK IDs | Walks structure at proxy-creation time | Baked at compile time from the semantic model |
| **Naming/policy** — table/column/FK names, constraints, cascades | **Calls `database.Configuration.GetXxx(...)` at runtime** inside the emitted methods | Same: call runtime config, **never bake names** |

Evidence this split is real: `IsRelatedItem(Type)` and `IsRelatedCollection(...)` are **non-virtual and structural** (`!type.IsValueType && ShouldMapTypeInternal(type)`; `IEnumerable<mapped-type>`) — fully replicable from a Roslyn semantic model. The virtual surface (`GetTableName`, `GetForeignKeyColumnName`, ...) is *naming/policy*, which the mapper and command builder already resolve through the runtime config everywhere else.

> The branch's mistake was over-baking. There is nothing to express in JSON if naming isn't baked — which dissolves the "customizations in JSON" problem entirely.

## Chosen approach: shape-only Roslyn source generator + registry/Emit hybrid

1. **New source-generator project** (`Watsonia.Data.Generator` as an incremental generator, not a console tool).
2. **Opt-in**: `[WatsoniaEntity]` attribute (added to the runtime lib) on entity classes — a generator only sees types in the *consuming compilation's source*, so scanning must be explicit, not ambient.
3. **Emitted per entity**: proxy class (`EntityProxy : Entity, IDynamicProxy`) overriding virtual properties for change tracking + lazy loading, value bag, `__SetValue`/`__GetValue`/`__SetValuesFromReader` — mirroring `Watsonia.Data.Reference` proxies and `Builder.cs` emission patterns. **No baked names**: table/column/FK names resolved via `database.Configuration` at call time, same as Emit code.
4. **Registration**: generated `[ModuleInitializer]` registers factories in a `DynamicProxyRegistry` (type → proxy type). `DynamicProxyFactory.GetDynamicProxy` checks the registry **first**, falls back to Reflection.Emit for non-generated entities (referenced-assembly entities, `ShouldMapType` edge cases).
5. **Salvage from `origin/generator`**: `Builder.cs` emission structure (508 lines), the generated `Reference/Entities/Proxies` examples, and the `WatsoniaGen` benchmark harness are directly reusable patterns.

## Open questions for the implementation session

- **`ShouldMapType` overrides** (shape-affecting virtual): registry/Emit fallback covers them, or an attribute can encode per-type decisions later.
- **Entities in referenced assemblies**: generators can't see them — Emit fallback serves them; documented limitation.
- **`ExportProxies`**: likely deprecated once generation is compile-time (its purpose was AOT-less environments).
- **Interceptors (C# 12)**: *phase 2* — rewrite `db.Query<T>()`/`db.Load<T>()` call sites AOT-safely (Dapper.AOT pattern). Complements proxies; not a config-execution mechanism.
- **If 100% config fidelity for shape is ever required**: EF compiled-model pattern — design-time tool that loads the compiled config via reflection and executes the real virtual methods. Deliberately deferred (build machinery, two-pass builds).

## Merge mechanics (important)

`git revert -m 1` was used, so if the old branch content is ever wanted wholesale, revert the revert first (or cherry-pick files). Generator branch content lives at `origin/generator`; the benchmark harness changes (6-framework run loop) were also reverted with it.
