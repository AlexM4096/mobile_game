# Arch.Unity Integration

This project uses **Arch.Unity** on top of Arch ECS.

When generating Arch code, prefer Arch.Unity integration APIs where they replace or extend default Arch behavior.

## Project Conventions

| Area                     | Use                                                    | Avoid / Notes                                                                                           |
| ------------------------ | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------------- |
| Systems                  | `UnitySystemBase`                                      | Do not use `BaseSystem<World, T>` directly for normal project systems                                   |
| Unity execution          | `ArchApp` / Arch.Unity `SystemRunner`                  | Do not manually recreate Unity `Update` managers for ECS systems                                        |
| Parallel ECS work        | `Arch.Unity.Jobs` + Unity C# Job System                | Do not prefer Arch's own parallel scheduler when Unity Jobs are appropriate                             |
| Burst jobs               | `[BurstCompile]` + Arch.Unity job interfaces           | Jobs must follow Burst/HPC# restrictions                                                                |
| GameObject → Entity      | `EntityConverter` / `IComponentConverter`              | Do not manually duplicate Arch.Unity conversion infrastructure                                          |
| Hybrid GameObjects       | `Sync With Entity` / `GameObjectReference` when needed | Avoid hybrid conversion when no GameObject synchronization is required                                  |
| Dependency injection     | Arch.Unity VContainer integration                      | Prefer the provided ArchApp/VContainer registration APIs over manually wiring `World`, app, and systems |
| Debugging                | Arch Hierarchy / Inspector                             | Prefer the supplied editor tooling when inspecting ECS state                                            |
| Source-generated systems | Never                                                  | Arch.Extended source generation is not used in this Unity project                                       |

## `UnitySystemBase`

Project systems use `UnitySystemBase`, provided by `Arch.Unity.Toolkit`.

```csharp
public sealed class MovementSystem : UnitySystemBase
{
    public MovementSystem(World world) : base(world)
    {
    }

    public override void Update(in SystemState state)
    {
        // System logic
    }
}
```

`UnitySystemBase` is the Unity-specific replacement for directly deriving from:

```csharp
BaseSystem<World, SystemState>
```

`SystemState` provides Unity-oriented runtime information such as time and delta time.

Do not infer that every system must use values from `SystemState`; inherit from `UnitySystemBase` regardless, and only consume the state when required.

## Unity Jobs

For parallel ECS processing, use `Arch.Unity.Jobs` and Unity's C# Job System.

```csharp
[BurstCompile]
public struct MovementJob : IJobArchChunk
{
    public int PositionId;

    public void Execute(NativeChunk chunk)
    {
        var positions = chunk.GetNativeArray<Position>(PositionId);

        for (int i = 0; i < positions.Length; i++)
        {
            var position = positions[i];

            // Process data.

            positions[i] = position;
        }
    }
}
```

Schedule it against an Arch query:

```csharp
var job = new MovementJob
{
    PositionId = Component<Position>.ComponentType.Id
};

job.ScheduleParallel(World, query).Complete();
```

Arch.Unity jobs integrate Arch chunks with Unity's Job System and are compatible with Burst.

### References
- [MovementSystem.cs](Assets\_Project\Develop\Runtime\Gameplay\Features\Movement\Systems\MovementSystem.cs)

### Job restrictions

Inside Arch.Unity jobs:

* use unmanaged/Burst-compatible data;
* do not use reference-type components;
* do not add/remove entities;
* do not add/remove components;
* do not perform other structural changes.

Arch.Unity currently does not provide a job-compatible Arch command buffer for these structural operations.

## GameObject Conversion

Use `EntityConverter` when authoring data on GameObjects that must become Arch entities.

For custom conversion, implement `IComponentConverter`:

```csharp
public sealed class HealthConverter
    : MonoBehaviour, IComponentConverter
{
    [SerializeField]
    private float value;

    public void Convert(IEntityConverter converter)
    {
        converter.AddComponent(new Health
        {
            Value = value
        });
    }
}
```

Arch.Unity supports two relevant conversion approaches:

```text
Convert And Destroy
→ Convert authoring GameObject data to ECS
→ Destroy the GameObject afterwards

Sync With Entity
→ Keep GameObject and Entity associated
→ Adds GameObjectReference
```

`EntityConverter` performs runtime conversion, unlike Unity Entities Subscenes. Avoid using conversion unnecessarily in performance-sensitive spawning paths when entities can be created directly.

## Hybrid Components

When a GameObject must remain associated with its entity, use Arch.Unity's existing hybrid integration rather than inventing another GameObject/entity mapping layer.

With `Sync With Entity`, Arch.Unity provides:

```csharp
GameObjectReference
```

to access the associated GameObject.

Do not add GameObject references to ECS components unless the feature actually requires hybrid behavior.

## `ArchApp`

Arch.Unity uses `ArchApp` to integrate the Arch `World` and systems with Unity's PlayerLoop.

Basic API:

```csharp
var app = ArchApp.Create();

app.AddSystems(systems =>
{
    systems.Add<MovementSystem>();
});

app.Run();
```

A specific Unity execution phase can use a `SystemRunner`:

```csharp
app.AddSystems(
    SystemRunner.FixedUpdate,
    systems =>
    {
        systems.Add<PhysicsSystem>();
    }
);
```

`ArchApp` owns the integration with Unity's PlayerLoop and supports different runners, including a `FakeSystemRunner` for tests.

Do not manually build an alternative ECS PlayerLoop integration when `ArchApp`/`SystemRunner` already covers the requirement.

## VContainer

This project uses the Arch.Unity VContainer integration.

Prefer the provided integration for creating and registering the Arch application, world, and systems.

Example:

```csharp
protected override void Configure(
    IContainerBuilder builder)
{
    builder.UseNewArchApp(
        Lifetime.Scoped,
        EntityConversion.DefaultWorld,
        systems =>
        {
            systems.Add<MovementSystem>();
            systems.Add<PhysicsSystem>();
        }
    );
}
```

The Arch.Unity VContainer integration registers the created `ArchApp`, `World`, and added systems with the DI container.

Do not manually duplicate those registrations unless the existing integration cannot represent the required configuration.

## Default Arch vs Arch.Unity

When examples from generic Arch documentation conflict with an Arch.Unity-specific integration, adapt them to this project.

```text
Generic Arch system
→ UnitySystemBase

Arch parallel scheduler
→ Arch.Unity.Jobs when using Unity parallel jobs

Manual GameObject → Entity glue
→ EntityConverter / IComponentConverter

Manual PlayerLoop integration
→ ArchApp / SystemRunner

Manual DI registration
→ Arch.Unity VContainer integration

Arch.Extended generated systems
→ Do not use
```

Use core Arch APIs normally for entities, components, worlds, and queries unless an Arch.Unity API specifically exists for the Unity integration concern.
