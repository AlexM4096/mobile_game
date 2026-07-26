# AGENTS.md

This file provides guidance to AI Agents when working with code in this repository.

## Project context

Unity 6 (`6000.3.19f1`) Solution stack:

- **Gameplay** — Arch ECS (Arch, Arch.System, Arch.Unity)
- **DI** — VContainer
- **Asynchronous programming** — UniTask (use everywhere, `Task`/`async void` — don't)
- **Reactive programming** — R3 (installed via NuGet for Unity) and R3.Unity (installed via git)
- **Content management** — Addressables
- **Animations via code** — PrimeTween (installed via scoped registry `npmjs.org`, scope `com.kyrylokuzyk`). Can work with UniTask from the box (`await tween`).
- **Text** — UniText (`media.lightside.unitext`, via scoped registry Light Side). Do not use TMP.
- **Sting** - ZString
- **Input** — new Input System (`UnityEngine.InputSystem`). Do not use `UnityEngine.Input.*` anywhere.
- **Inspector / attributes** — Odin Inspector.
- **Rendering** - URP 2D

>When adding a new technology/framework to the project, the user will expand this section. If you see a technology in the code that isn't on this list, ask the user if it should be added here.

## Project structure

`Assets/_Project/` — project directtory **with all user code and resources**. Other files in `Assets/` are imported packages, plugins and assets; this separation is necessary to avoid confusing project assets with external ones. The underscore in the name keeps `_Project` at the top of the Project window—keep it in the namespace.

```
Assets/_Project/
├── Develop/                    # ALL project code
│   ├── Editor/                 # Editor-only tools
│   └── Runtime/                # Runtime code
│       ├── EntryPoint/         # GameEntryPoint, RootLifetimeScope
│       ├── EntitiesCore/       # ECS infrastructure
│       ├── Gameplay/           # Core of the game
│       │   ├── Infrastructure/ # GameplayBootstrap, GameplayLifetimeScope
│       │   └── Features/       # One folder per feature
│       ├── Meta/               # META mechanics
│       │   ├── Infrastructure/ # MainMenuBootstrap and etc
│       │   └── Features/       # Will appear as meta mechanics are developed
│       └── Utilities/          # Services
├── AddressablesResources/      # Prefabs/assets via Addressables
├── Resources/                  # Only what is loaded synchronously at startup
├── Scenes/                     # GameEntryPointScene, EmptyScene, MainMenuScene, GameplayScene
└── Art/                        # Art assets
```

**Parallelism between `Gameplay/` and `Meta/`** are two game domains of the same level. Both follow the same pattern: the `Infrastructure/` subfolder for bootstraps/installers for their scene and the `Features/` subfolder for feature division. When meta-mechanics (currencies, progression, battle passes) appear in the game, their features are placed in `Meta/Features/`, following the same pattern as gameplay features. Don't mix meta-logic in `Gameplay/Features/` and vice versa.

**Resources vs Addressables.** Addressables is a default option. `Resources/` should contain only what must be available at startup without asynchronous loading and justifies the increase in build size.
