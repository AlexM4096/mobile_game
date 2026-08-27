# Singleton Handling via VContainer

When working with Unity APIs that expose global or singleton-like access, prefer resolving them through **VContainer** rather than repeatedly accessing the static/global API.

Examples include:

* `Camera.main`
* `EventSystem.current`
* Other scene-level objects commonly accessed through static properties or lookup methods

## Preferred approach

Register the instance once in the relevant VContainer `LifetimeScope`, then inject it into consumers.

Prefer explicitly assigning the dependency when practical:

```csharp
public sealed class GameplayLifetimeScope : LifetimeScope
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private EventSystem eventSystem;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(mainCamera);
        builder.RegisterInstance(eventSystem);
    }
}
```

Consumers should receive the dependency through constructor or method injection:

```csharp
public sealed class PlayerInputHandler
{
    private readonly Camera _camera;
    private readonly EventSystem _eventSystem;

    public PlayerInputHandler(
        Camera camera,
        EventSystem eventSystem
    )
    {
        _camera = camera;
        _eventSystem = eventSystem;
    }
}
```

## Registering Unity singleton access directly

It is also acceptable to resolve a Unity singleton/global instance during container configuration and register it directly:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    builder.RegisterInstance(Camera.main);
    builder.RegisterInstance(EventSystem.current);
}
```

This is valid and still preferable to having every consumer access `Camera.main` or `EventSystem.current` independently.

However, it is **slightly less preferred** than explicitly assigning the instance to the `LifetimeScope`, because the dependency is less explicit in the scene/configuration and the lookup is hidden inside container setup.

Preferred order:

1. Explicitly assigned instance registered with VContainer.
2. Singleton/global instance resolved once during VContainer configuration and registered.
3. Singleton/global lookup cached by the consumer.
4. Repeated singleton/global or object lookup calls.

## Caching

If an instance must be obtained through a Unity singleton or lookup API, prefer resolving it once and caching the result rather than performing the lookup repeatedly.

Prefer:

```csharp
private readonly Camera _camera;

public SomeSystem()
{
    _camera = Camera.main;
}
```

over:

```csharp
public void Update()
{
    var camera = Camera.main;
}
```

Caching through a field is preferred, but exposing or retrieving an already-cached instance through a property/getter is also acceptable.

## Lookup APIs

Using Unity lookup APIs is allowed when dependency injection is impractical, especially for temporary, editor, initialization, or legacy code.

Examples include:

```csharp
Camera.main
EventSystem.current
FindObjectOfType<T>()
FindFirstObjectByType<T>()
FindAnyObjectByType<T>()
```

However, these approaches are **not preferred for normal runtime dependencies**.

When the dependency is known during composition/setup:

1. Register it with VContainer.
2. Inject it where needed.
3. Cache and reuse the injected reference.

Do not introduce custom static singleton wrappers merely to avoid dependency injection when VContainer can own and provide the dependency.
