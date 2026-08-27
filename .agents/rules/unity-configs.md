# Config Design Rule

When creating configuration/data classes in Unity, use one of these two patterns:

1. **ScriptableObject config**
2. **Plain serializable C# config**

## Default choice

If the user does **not** specify which type to use, default to a **ScriptableObject**.

Use a plain C# class only when the user explicitly requests it or when the config clearly needs to be embedded inside another serialized object rather than exist as a standalone asset.

## General rules

For both config types:

* Serialized fields must be `private`.
* Expose values through public getter-only properties.
* Do not expose mutable collections such as `List<T>` publicly.
* Expose collections as `IReadOnlyList<T>` or another appropriate read-only interface.
* Prefer clear configuration-focused names such as `MovementConfig`, `EnemyConfig`, or `WeaponConfig`.
* Avoid behavior and runtime state in config objects unless explicitly required.
* Do not add public setters just to make serialization easier.
* Use `[SerializeField]` for Unity-serialized private fields.

---

## ScriptableObject config

A normal ScriptableObject config must:

* Inherit from `ScriptableObject`.
* Include `[CreateAssetMenu]`.
* Keep fields private and serialized.
* Expose getter-only properties.
* Expose collections through read-only interfaces.

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponConfig",
    menuName = "Configs/Weapon Config"
)]
public sealed class WeaponConfig : ScriptableObject
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private List<string> allowedTargets = new();

    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public IReadOnlyList<string> AllowedTargets => allowedTargets;
}
```

Do **not** expose the underlying list:

```csharp
// Avoid
public List<string> AllowedTargets => allowedTargets;
```

Prefer:

```csharp
public IReadOnlyList<string> AllowedTargets => allowedTargets;
```

---

## Plain C# config

A plain C# config must:

* Include `[System.Serializable]`.
* Not inherit from `ScriptableObject`.
* Keep fields private and serialized.
* Expose getter-only properties.
* Expose collections through read-only interfaces.

```csharp
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public sealed class WeaponConfig
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private List<string> allowedTargets = new();

    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public IReadOnlyList<string> AllowedTargets => allowedTargets;
}
```

A plain config can then be embedded inside another serialized object:

```csharp
using UnityEngine;

public sealed class Weapon : MonoBehaviour
{
    [SerializeField]
    private WeaponConfig config;

    public WeaponConfig Config => config;
}
```

---

## Odin Inspector

The project has **Odin Inspector**, but do not use Odin-specific APIs unless the user requests Odin or asks for an improved/custom editor experience.

When Odin is requested:

* Use Odin attributes where they improve editor readability or usability.
* Prefer attributes such as `[Title]`, `[BoxGroup]`, `[FoldoutGroup]`, `[MinValue]`, `[MaxValue]`, `[Required]`, `[PreviewField]`, etc. when appropriate.
* Do not add Odin attributes without a practical editor benefit.
* For ScriptableObject configs, inherit from `SerializedScriptableObject` instead of `ScriptableObject`.

Example:

```csharp
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponConfig",
    menuName = "Configs/Weapon Config"
)]
public sealed class WeaponConfig : SerializedScriptableObject
{
    [Title("Combat")]
    [MinValue(0)]
    [SerializeField]
    private float damage = 10f;

    [MinValue(0)]
    [SerializeField]
    private float attackCooldown = 0.5f;

    [Title("Targeting")]
    [SerializeField]
    private List<string> allowedTargets = new();

    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public IReadOnlyList<string> AllowedTargets => allowedTargets;
}
```

Use Odin serialization-specific attributes such as `[OdinSerialize]` only when Odin serialization is actually needed. Standard Unity-serializable fields should generally continue using `[SerializeField]`.

## Decision rule

Unless the user specifies otherwise:

```text
Need a config
    ↓
Did the user explicitly request a plain C# class?
    ├─ Yes → [System.Serializable] plain C# config
    └─ No  → ScriptableObject config with [CreateAssetMenu]

Did the user request Odin?
    ├─ Yes → Use appropriate Odin attributes
    │         and SerializedScriptableObject for SO configs
    └─ No  → Use standard Unity serialization
```
