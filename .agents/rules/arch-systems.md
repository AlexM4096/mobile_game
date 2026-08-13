# Arch Systems

All ECS systems registered in `ArchApp` must inherit from `UnitySystemBase`.

A system must own **exactly one ECS query**. If logic requires multiple distinct queries, split it into multiple systems.

Naming and feature placement are defined in `rules/arch-features.md`.

## System Rules

| Area                  | Rule                                                                                   |
| --------------------- | -------------------------------------------------------------------------------------- |
| Base class            | Inherit from `UnitySystemBase`                                                         |
| Responsibility        | One system represents one clear ECS operation                                          |
| Query count           | Exactly **one query per system**                                                       |
| Query description     | Name it `_description`                                                                 |
| Query implementation  | Follow `rules/arch-query.md`                                                           |
| Constructor           | Receive `World` and injected dependencies                                              |
| Dependencies          | Store injected dependencies in `private readonly` fields                               |
| DI                    | Use constructor injection; do not resolve VContainer services inside lifecycle methods |
| `SystemState`         | Use only when the chosen update method needs data provided by it                       |
| Update method         | Override **one** of `BeforeUpdate`, `Update`, or `AfterUpdate`                         |
| Default update method | Prefer regular `Update` unless ordering requires `BeforeUpdate` or `AfterUpdate`       |
| `Initialize`          | Optional one-time initialization                                                       |
| `Dispose`             | Optional cleanup for resources owned by the system                                     |
| Unity lifecycle       | Do not use `MonoBehaviour` lifecycle methods for ECS system execution                  |
| Naming / placement    | Follow `rules/arch-features.md`                                                        |
| System ordering       | Defined by feature registration; see `rules/arch-features.md`                          |

## Canonical System

```csharp
public sealed class MoveCharacterSystem : UnitySystemBase
{
    private static readonly QueryDescription _description =
        new QueryDescription()
            .WithAll<Position, Velocity>();

    private readonly MovementConfig _config;

    public MoveCharacterSystem(
        World world,
        MovementConfig config)
        : base(world)
    {
        _config = config;
    }

    public override void Update(in SystemState state)
    {
        // Execute the system's single query.
        // Query style follows rules/arch-query.md.
    }
}
```

Use `Update` by default.

Choose `BeforeUpdate` or `AfterUpdate` instead only when the system specifically needs to execute in that phase.

Do not implement multiple update-phase methods in the same system.

If a system needs a second `QueryDescription`, create another system instead.
