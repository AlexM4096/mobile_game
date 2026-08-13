# Arch ECS Query Selection

When the user explicitly requests a query type, use it unless it is incompatible with the requested system.

If the user does not specify a query type, choose from the table below.

| Query                  | Use when                                                                                                           | Do not use when                                                                                              |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------ |
| `InlineQuery`          | Default for runtime systems when only component data is required                                                   | The query needs `SystemState`, `Time`, `DeltaTime`, or other state that cannot be passed to the inline query |
| `InlineEntityQuery`    | Same as `InlineQuery`, but the `Entity` itself is required                                                         | The entity is not used, or the query requires unavailable system state                                       |
| `World.Query`          | The query needs `SystemState`, `Time`, `DeltaTime`, captured values, or is editor/debug/setup/rarely executed code | A normal hot runtime loop can cleanly use `InlineQuery` instead                                              |
| Custom Enumeration     | Profiling or the workload justifies lower-level chunk iteration; specialized chunk access is required              | As a default replacement for simpler query APIs                                                              |
| Bulk `World` operation | Every matching entity receives the same supported operation, such as destroy/add/remove/set                        | Per-entity custom logic is required                                                                          |
| Source-Generated Query | Never                                                                                                              | Always. Source generation is not a valid option in this Unity project                                        |

## `InlineQuery`

Preferred default for normal runtime iteration.

```csharp
private struct UpdatePositionQuery : IForEach<Position, Velocity>
{
    public void Update(ref Position position, ref Velocity velocity)
    {
        position.Value += velocity.Value;
    }
}

private readonly QueryDescription _description =
    new QueryDescription()
        .WithAll<Position, Velocity>();

public override void Update()
{
    World.InlineQuery<UpdatePositionQuery, Position, Velocity>(in _description);
}
```

Use `InlineEntityQuery` when the entity is actually needed:

```csharp
private struct UpdateQuery : IForEachWithEntity<Position>
{
    public void Update(Entity entity, ref Position position)
    {
        // Entity is required by the logic.
    }
}

World.InlineEntityQuery<UpdateQuery, Position>(in _description);
```

Do not choose an entity query only because the entity might be useful later.

## `World.Query`

Use when the query logic requires values from the surrounding system, especially `SystemState`.

For example, a `UnitySystemBase` that needs delta time:

```csharp
public override void Update(ref SystemState state)
{
    float deltaTime = state.Time.DeltaTime;

    World.Query(
        in _description,
        (ref Position position, ref Velocity velocity) =>
        {
            position.Value += velocity.Value * deltaTime;
        }
    );
}
```

In this case, do not force the system into `InlineQuery` just for performance consistency.

`World.Query` is also acceptable for editor tools, debugging, initialization, setup, or rarely executed code.

## Custom Enumeration

Use only when lower-level iteration has a concrete reason.

```csharp
var query = World.Query(in _description);

foreach (ref var chunk in query.GetChunkIterator())
{
    var components = chunk.GetFirst<Position, Velocity>();

    foreach (var index in chunk)
    {
        ref var position = ref Unsafe.Add(ref components.t0, index);
        ref var velocity = ref Unsafe.Add(ref components.t1, index);

        position.Value += velocity.Value;
    }
}
```

Choose this when:

* profiling identifies the query as a hot path;
* direct chunk access is useful;
* the iteration pattern cannot be expressed efficiently with the higher-level APIs.

Do not introduce Custom Enumeration purely because it benchmarks faster in isolation.

## Bulk Operations

If the same supported operation applies to the entire matching query, prefer the corresponding bulk API instead of iterating entities manually.

```csharp
World.Destroy(in _description);
```

Prefer this over:

```csharp
World.Query(
    in _description,
    (Entity entity) =>
    {
        World.Destroy(entity);
    }
);
```

Use bulk APIs only when no per-entity custom logic is required.

## Source Generation

Do not use Arch source-generated queries in this project.

Do not generate or recommend patterns such as:

```csharp
[Query]
private void UpdatePosition(ref Position position, ref Velocity velocity)
{
}
```

If Arch documentation presents source generation as an optimization, choose the closest valid option instead:

```text
Normal runtime query
→ InlineQuery / InlineEntityQuery

Needs SystemState / Time / DeltaTime
→ World.Query

Proven hot path
→ Custom Enumeration
```
