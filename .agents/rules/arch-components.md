# Arch Components

Components belonging to the same feature must be declared together in:

```text
Features/<Name>/<Name>FeatureComponents.cs
```

Feature naming and placement follow `rules/arch-features.md`.

Do not create one file per component unless there is a strong reason to separate it.

## Component Rules

| Area                    | Rule                                                                                                  |
| ----------------------- | ----------------------------------------------------------------------------------------------------- |
| Location                | Put feature-owned components in `<Name>FeatureComponents.cs`; see `rules/arch-features.md`            |
| Scope                   | Keep components with the feature that owns them                                                       |
| Naming                  | Use concise domain names such as `Health`, `Damage`, `MoveSpeed`, `DamageRequest`, `EnemyKilledEvent` |
| Single-field component  | Prefer a one-line declaration                                                                         |
| Single-field field name | Prefer `Value` or `Amount`                                                                            |
| Multi-field component   | Declare normally with one field per line                                                              |
| Tags                    | Use empty structs                                                                                     |
| Requests                | Keep request components in the same feature components file                                           |
| Events                  | Keep event components in the same feature components file                                             |
| Data only               | Components should contain data, not system behavior                                                   |
| References              | Avoid managed/reference-type fields unless explicitly required                                        |

## Preferred Style

Most simple components should be declared in one line:

```csharp
public struct Health { public float Value; }
public struct MaxHealth { public float Value; }
public struct Damage { public float Amount; }
public struct MoveSpeed { public float Value; }
```

Use `Value` for a generic wrapped value.

Use `Amount` when the component represents a quantity.

## Tags, Requests, and Events

Tags contain no data:

```csharp
public struct PlayerTag { }
public struct DeadTag { }
```

Requests represent work that should be processed by a system:

```csharp
public struct RespawnRequest { }

public struct DamageRequest
{
    public Entity Target;
    public float Amount;
}
```

Events represent something that has already happened:

```csharp
public struct EnemyKilledEvent { }

public struct DamageAppliedEvent
{
    public Entity Target;
    public float Amount;
}
```

Tags, requests, and events belong in the same `<Name>FeatureComponents.cs` file as the rest of the feature components.

Their systems and feature placement follow `rules/arch-systems.md` and `rules/arch-features.md`.

## Example Feature Components File

```csharp
public struct Health { public float Value; }
public struct MaxHealth { public float Value; }
public struct Damage { public float Amount; }

public struct PlayerTag { }
public struct DeadTag { }

public struct RespawnRequest { }
public struct EnemyKilledEvent { }

public struct DamageRequest
{
    public Entity Source;
    public Entity Target;
    public float Amount;
}

public struct MovementInput
{
    public float Horizontal;
    public float Vertical;
}
```

Prefer concise one-line declarations for single-field and empty components. Use the normal multi-line form when a component contains multiple fields.
