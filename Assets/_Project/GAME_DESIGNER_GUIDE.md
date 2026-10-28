# Game Designer Workflow

This guide covers the current gameplay prototype and its balance configs.

## Quick start

1. Open `Assets/_Project/Scenes/MovementSwarmTestScene.unity`.
2. In the Hierarchy, select `Movement Swarm Test Lifetime Scope`.
3. Change the required config values in the Inspector. The main configs are shown inline on this object.
4. Enter Play Mode to test the result.

Controls:

- Press and hold the mouse button or touch the screen to orbit around that point.
- Release to launch in the current direction.

For repeatable results, stop Play Mode before editing configs and enter Play Mode again after each change. Restarting resets the player, enemies, random seed, and wave timeline.

## Where the configs are

Config assets are stored in:

`Assets/_Project/AddressablesResources/<Feature>/Configs/`

Most balance work can be done from the inline config fields on `Movement Swarm Test Lifetime Scope`. You can also select an individual config asset in the Project window.

### Player

- `Infrastructure/Configs/SwarmConfig` — player health, collider radius, and player prefab.
- `Player/Configs/PlayerMovementConfig` — orbit speed, launch speed, deceleration, minimum speed, bounce speed gain, and maximum speed.
- `Player/Configs/PlayerCameraConfig` — whether the camera locks to the orbit centre and how smoothly it follows its target.
- `Player/Configs/PlayerOrbitVisualizationConfig` — orbit line color, line width, and marker size.
- `Movement/Configs/MovementConfig` — arrival distance used when moving entities toward a target. This usually does not need regular balance changes.

`PlayerInputConfig` contains Input System action references. Treat these as setup references, not balance values.

Disable `Lock To Orbit Center` to keep the camera following the player during orbit. `Damping` controls the smooth transition between the player and orbit centre; `0` makes camera movement immediate.

### Enemies and waves

- `EnemySpawn/Configs/EnemySpawnConfig` — random seed, the shared equipment visual catalog reference, and the wave list.
- `EnemySpawn/Configs/EnemyEquipmentVisualConfig` — shared weapon and helmet sprite bindings. This is setup data and normally should not be changed for balancing.
- `EnemySpawn/Configs/Enemies/` — individual enemy configs.

Each wave contains:

- `Delay` — pause before the wave starts. For later waves, it begins after the previous wave's duration.
- `Duration` — the timeline length before the next wave can start. Enemies in the wave use their own spawn intervals.
- `Spawn Zone` — circle or box, its position and size, and whether enemies spawn on its edge.
- `Composition` — enemy type, total count, interval between its spawns, and that entry's equipment loadout.

Important enemy values:

- `Stat Noise Percentage` adds seeded random variation to an enemy's movement speed. `0` disables variation; `0.2` means up to roughly 20% variation.
- `Weapon Type` is selected on each wave composition entry. Staff, Dagger, and Axe are visually different but each adds one defensive hit layer before the enemy body.
- `Helmet Type` is selected on each wave composition entry. Cloth takes 1 hit, Wood 2, Iron 3, and Dragon Scale 4.
- `Enchanted Armor` is also selected on the wave entry and adds one hit of helmet durability. It is shown only when `Helmet Type` is not `None`.

Equipment is intentionally wave-specific. Reusing the same enemy config in two composition entries can therefore produce different weapon and helmet combinations without creating another enemy asset.

Using the same random seed and the same config values produces the same spawn positions and stat variations.

### Sword

`Sword/Configs/SwordConfig` controls the sword prefab, orbit speed, orbit radius, direction, weapon count, damage, and hitbox size.

Keep `Weapon Count` at 1 or higher, and keep speed, radius, damage, and both hitbox dimensions above zero.

### Spear

`Spear/Configs/SpearConfig` controls the automatic linear Spear ability. `Enabled` is the test toggle: disable it to run the scene without Spear targeting or projectile creation.

The default prototype targets immediately and then every 2 seconds. It locks a direction for 0.25 seconds before firing, prefers distinct visible enemies inside its player-centred trigger zone, and chooses a random direction when no eligible enemy is present. The effective trigger radius is clamped so its circle remains inside the camera view.

`Projectile Count` controls simultaneous shots. `Thickness Multiplier` is multiplied by the gameplay Grid cell height, while the prototype visual and collider remain 1.5 tiles long. `Damage Hits` is the number of enemy defensive layers removed per unique contact. `Total Health` is the number of unique enemies a projectile can pierce before destruction. `Projectile Lifetime` removes missed projectiles through the shared lifetime pipeline.

### Visuals and debugging

- `Movement/Configs/WalkingTweenConfig` controls the shared walking squash, stretch, rotation, timing, easing, and variation used by player and enemy prefabs.
- `Health/Configs/HealthBarConfig` controls health-bar offset and size.
- `Collision/Configs/ColliderDebugConfig` enables collider outlines and controls their widths and per-layer colors.

In `WalkingTweenConfig`, `Noise` varies animation timing, pose strength, and starting phase between characters. `0` makes their animations uniform; higher values make them less synchronized.

## Safe editing rules

- Change numeric and enum balance values freely within the limits shown by the Inspector.
- Do not replace prefab, input-action, camera, Event System, or config references unless you are intentionally changing scene setup.
- Do not remove every wave or every composition entry from a wave.
- Use a positive wave duration and valid spawn-zone size.
- If an entry spawns more than one enemy, use a spawn interval above zero.
- Test structural wave changes from the beginning by stopping and restarting Play Mode.

## Troubleshooting

- If nothing spawns, open the Unity Console and check for an invalid wave, composition entry, spawn zone, or missing prefab.
- If the player or sword does not appear, check the prefab references in `SwarmConfig` and `SwordConfig`.
- If controls do not work, confirm that `PlayerInputConfig` still has its point and press actions assigned.
- If a change appears to have no effect, stop and restart Play Mode; systems may have already initialized or scheduled the current wave.
- If red collider outlines or other debug shapes are unwanted, disable `ColliderDebugConfig` rather than changing collision setup.
