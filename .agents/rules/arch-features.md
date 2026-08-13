# Arch Features

Each feature lives under:

```text
Features/<Name>/
├── <Name>Feature.cs
├── <Name>FeatureComponents.cs
├── Systems/
│   ├── <Verb><Subject>System.cs
│   └── ...
├── <Name>View.cs
├── <Name>Registrator.cs
└── <Name>Factory.cs
```

Optional files are created only when required.

## Feature Structure

| File                               | Purpose                                               |
| ---------------------------------- | ----------------------------------------------------- |
| `<Name>Feature.cs`                 | Extension method that registers the feature's systems |
| `<Name>FeatureComponents.cs`       | Feature components; follow `rules/arch-components.md` |
| `Systems/<Verb><Subject>System.cs` | Feature systems; follow `rules/arch-systems.md`       |
| `<Name>View.cs`                    | Optional Unity view                                   |
| `<Name>Registrator.cs`             | Optional feature-specific registration/helper         |
| `<Name>Factory.cs`                 | Optional factory for spawning feature entities        |

## Registration Rules

| Area                   | Rule                                                                                      |
| ---------------------- | ----------------------------------------------------------------------------------------- |
| Feature entry point    | Define an extension method on `NewArchAppBuilder` in `<Name>Feature.cs`                   |
| System registration    | Register systems with `systems.Add<T>()`                                                  |
| Registration style     | Systems may be registered individually or grouped behind a feature extension              |
| Execution order        | Registration order defines system order                                                   |
| Runner                 | Use the `Add<T>(ISystemRunner)` overload when a system requires a specific runner         |
| Dependencies           | Register configs, factories, services, and other dependencies normally through VContainer |
| Feature responsibility | A feature extension groups only the systems belonging to that feature                     |
| Composition            | Register systems and features inside `UseNewArchApp(...)`                                 |
| System implementation  | Follow `rules/arch-systems.md`                                                            |
| Components             | Follow `rules/arch-components.md`                                                         |
| Queries                | Follow `rules/arch-query.md`                                                              |

## Example Registration

A composition root may mix standalone systems and grouped features.

```csharp
public sealed class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private MovementConfig movementConfig;
    [SerializeField] private CombatConfig combatConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        // Normal VContainer registrations.
        builder.RegisterInstance(movementConfig);
        builder.RegisterInstance(combatConfig);

        builder.Register<ProjectileFactory>(Lifetime.Scoped);
        builder.Register<EnemyFactory>(Lifetime.Scoped);

        builder.UseNewArchApp(
            Lifetime.Scoped,
            EntityConversion.DefaultWorld,
            systems =>
            {
                // Standalone system.
                systems.Add<InitializeGameSystem>();

                // Grouped feature.
                systems.AddMovementFeature();

                // Another standalone system.
                systems.Add<UpdateCameraTargetSystem>();

                // Another grouped feature.
                systems.AddCombatFeature();

                // System using a specific runner.
                systems.Add<SimulatePhysicsSystem>(SystemRunner.FixedUpdate);

                // Final standalone cleanup system.
                systems.Add<CleanupDestroyedEntitiesSystem>();
            }
        );
    }
}

public static class MovementFeature
{
    public static void AddMovementFeature(this NewArchAppBuilder systems)
    {
        systems.Add<ReadMovementInputSystem>();
        systems.Add<MoveCharacterSystem>();
        systems.Add<SyncCharacterViewSystem>();
    }
}

public static class CombatFeature
{
    public static void AddCombatFeature(this NewArchAppBuilder systems)
    {
        systems.Add<CollectAttackRequestsSystem>();
        systems.Add<ApplyDamageSystem>();
        systems.Add<ProcessDeathSystem>();
    }
}
```

The effective system order is therefore:

```text
InitializeGameSystem

MovementFeature
├── ReadMovementInputSystem
├── MoveCharacterSystem
└── SyncCharacterViewSystem

UpdateCameraTargetSystem

CombatFeature
├── CollectAttackRequestsSystem
├── ApplyDamageSystem
└── ProcessDeathSystem

SimulatePhysicsSystem        [FixedUpdate]
CleanupDestroyedEntitiesSystem
```

Feature extensions exist to make related system registration reusable and readable. They do not create a separate execution model; each `systems.Add<T>()` still registers the system into the same `ArchApp`.

## Boundaries

`<Name>Feature.cs` defines:

* which systems belong to the feature;
* their registration order;
* which runner each system uses.

Configs, factories, registrators, and other dependencies are registered through normal VContainer registration.

Do not pass dependencies through `NewArchAppBuilder` unless they are actually required to decide how systems are registered.
