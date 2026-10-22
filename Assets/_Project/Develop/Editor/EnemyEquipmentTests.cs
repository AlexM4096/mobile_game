using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.EnemySpawn.Systems;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Views;
using _Project.Gameplay.Features.Player;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class EnemyEquipmentTests
    {
        private const string CatalogPath =
            "Assets/_Project/AddressablesResources/EnemySpawn/Configs/EnemyEquipmentVisualConfig.asset";
        private const string SpriteSheetPath =
            "Assets/_Project/Art/prototype/enemy_spr_set.png";
        private const string EnemyPrefabFolder =
            "Assets/_Project/AddressablesResources/EnemySpawn/Prefabs/Enemies";

        [Test]
        public void CatalogMapsEveryEquipmentTypeToTheExpectedSprite()
        {
            var catalog = LoadCatalog();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
                .OfType<Sprite>()
                .ToDictionary(sprite => sprite.name);

            Assert.That(catalog.GetWeaponSprite(WeaponType.None), Is.Null);
            Assert.That(catalog.GetWeaponSprite(WeaponType.Staff), Is.SameAs(sprites["enemy_spr_set 1_4"]));
            Assert.That(catalog.GetWeaponSprite(WeaponType.Dagger), Is.SameAs(sprites["enemy_spr_set 1_5"]));
            Assert.That(catalog.GetWeaponSprite(WeaponType.Axe), Is.SameAs(sprites["enemy_spr_set 1_6"]));
            Assert.That(catalog.GetHelmetSprite(HelmetType.None), Is.Null);
            Assert.That(catalog.GetHelmetSprite(HelmetType.Cloth), Is.SameAs(sprites["enemy_spr_set 1_7"]));
            Assert.That(catalog.GetHelmetSprite(HelmetType.Wood), Is.SameAs(sprites["enemy_spr_set 1_8"]));
            Assert.That(catalog.GetHelmetSprite(HelmetType.Iron), Is.SameAs(sprites["enemy_spr_set 1_9"]));
            Assert.That(
                catalog.GetHelmetSprite(HelmetType.DragonScale),
                Is.SameAs(catalog.GetHelmetSprite(HelmetType.Iron)));
        }

        [Test]
        public void EveryEnemyPrefabHasConfiguredSocketsUnderVisual()
        {
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder });
            Assert.That(prefabGuids, Is.Not.Empty);

            foreach (var prefabGuid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var view = prefab.GetComponent<EnemyDefenseView>();

                Assert.That(view, Is.Not.Null, path);
                Assert.That(view.TryValidate(out var error), Is.True, $"{path}: {error}");
                Assert.That(view.HelmetSocket.parent, Is.SameAs(view.BodyRenderer.transform));
                Assert.That(view.WeaponSocket.parent, Is.SameAs(view.BodyRenderer.transform));
            }
        }

        [Test]
        public void ViewUsesSelectedSpritesAtBodyOriginAndAboveBody()
        {
            var catalog = LoadCatalog();
            var root = new GameObject("Enemy Equipment View Test");

            try
            {
                var view = CreateConfiguredView(
                    root,
                    out var body,
                    out var helmetSocket,
                    out var weaponSocket);
                body.sortingOrder = 4;
                body.flipX = true;
                body.flipY = true;

                view.Initialize(
                    WeaponType.Axe,
                    catalog.GetWeaponSprite(WeaponType.Axe),
                    HelmetType.Cloth,
                    catalog.GetHelmetSprite(HelmetType.Cloth),
                    1,
                    1,
                    false);

                var weapon = FindRenderer(weaponSocket, "Weapon Layer");
                var helmet = FindRenderer(helmetSocket, "Helmet Layer");
                Assert.That(weapon.sprite, Is.SameAs(catalog.GetWeaponSprite(WeaponType.Axe)));
                Assert.That(helmet.sprite, Is.SameAs(catalog.GetHelmetSprite(HelmetType.Cloth)));
                Assert.That(weapon.transform.parent, Is.SameAs(weaponSocket));
                Assert.That(helmet.transform.parent, Is.SameAs(helmetSocket));
                Assert.That(weapon.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(helmet.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(weapon.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(helmet.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(weapon.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(helmet.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(weapon.sortingOrder, Is.EqualTo(5));
                Assert.That(helmet.sortingOrder, Is.EqualTo(6));
                Assert.That(weapon.sortingLayerID, Is.EqualTo(body.sortingLayerID));
                Assert.That(helmet.sortingLayerID, Is.EqualTo(body.sortingLayerID));
                Assert.That(weapon.flipX, Is.True);
                Assert.That(weapon.flipY, Is.True);
                Assert.That(helmet.flipX, Is.True);
                Assert.That(helmet.flipY, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void NoneEquipmentCreatesNoGearRenderers()
        {
            var root = new GameObject("Unequipped Enemy View Test");

            try
            {
                var view = CreateConfiguredView(root, out _, out _, out _);
                view.Initialize(
                    WeaponType.None,
                    null,
                    HelmetType.None,
                    null,
                    0,
                    0,
                    false);

                Assert.That(view.HasWeaponVisual, Is.False);
                Assert.That(view.HasHelmetVisual, Is.False);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HelmetVisualUsesThreeStatesAndBlendsDragonEnchantment()
        {
            var catalog = LoadCatalog();
            var root = new GameObject("Helmet State View Test");

            try
            {
                var view = CreateConfiguredView(root, out _, out var helmetSocket, out _);
                view.Initialize(
                    WeaponType.None,
                    null,
                    HelmetType.DragonScale,
                    catalog.GetHelmetSprite(HelmetType.DragonScale),
                    5,
                    5,
                    true);

                var helmet = FindRenderer(helmetSocket, "Helmet Layer");
                var socketScale = helmetSocket.localScale;
                var socketRotation = helmetSocket.localRotation;
                var dragonTint = new Color(0.35f, 0.82f, 0.42f, 1f);
                var enchantedTint = new Color(0.75f, 0.25f, 1f, 1f);
                AssertColor(helmet.color, Color.Lerp(dragonTint, enchantedTint, 0.3f));
                Assert.That(view.CurrentHelmetState, Is.EqualTo(HelmetVisualState.FullDurability));

                view.ApplyState(3, BrokenDefenseLayers.None);
                Assert.That(view.CurrentHelmetState, Is.EqualTo(HelmetVisualState.Damaged));
                Assert.That(helmet.transform.localScale, Is.EqualTo(new Vector3(0.9f, 0.9f, 1f)));
                Assert.That(helmet.transform.localEulerAngles.z, Is.EqualTo(7f).Within(0.01f));

                view.ApplyState(1, BrokenDefenseLayers.None);
                Assert.That(view.CurrentHelmetState, Is.EqualTo(HelmetVisualState.OneHitRemaining));
                Assert.That(helmet.transform.localScale, Is.EqualTo(new Vector3(0.8f, 0.8f, 1f)));
                Assert.That(helmet.transform.localEulerAngles.z, Is.EqualTo(349f).Within(0.01f));
                Assert.That(helmetSocket.localScale, Is.EqualTo(socketScale));
                Assert.That(helmetSocket.localRotation, Is.EqualTo(socketRotation));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EquipmentInheritsSocketAndAnimatedVisualTransforms()
        {
            var catalog = LoadCatalog();
            var root = new GameObject("Equipment Socket Transform Test");

            try
            {
                var view = CreateConfiguredView(
                    root,
                    out var body,
                    out _,
                    out var weaponSocket);
                weaponSocket.localPosition = new Vector3(0.25f, 0.4f, 0f);
                weaponSocket.localRotation = Quaternion.Euler(0f, 0f, 12f);
                weaponSocket.localScale = new Vector3(1.2f, 0.8f, 1f);

                view.Initialize(
                    WeaponType.Staff,
                    catalog.GetWeaponSprite(WeaponType.Staff),
                    HelmetType.None,
                    null,
                    0,
                    0,
                    false);

                var weapon = FindRenderer(weaponSocket, "Weapon Layer");
                Assert.That(weapon.transform.position, Is.EqualTo(weaponSocket.position));
                Assert.That(weapon.transform.rotation, Is.EqualTo(weaponSocket.rotation));
                Assert.That(weapon.transform.lossyScale, Is.EqualTo(weaponSocket.lossyScale));

                body.transform.localScale = new Vector3(1.5f, 0.75f, 1f);
                body.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                Assert.That(weapon.transform.position, Is.EqualTo(weaponSocket.position));
                Assert.That(weapon.transform.rotation, Is.EqualTo(weaponSocket.rotation));
                Assert.That(weapon.transform.lossyScale, Is.EqualTo(weaponSocket.lossyScale));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BrokenEquipmentDetachesFromSocketAndKeepsItsWorldTransform()
        {
            var catalog = LoadCatalog();
            var root = new GameObject("Equipment Break Socket Test");
            GameObject brokenPiece = null;

            try
            {
                var view = CreateConfiguredView(root, out _, out _, out var weaponSocket);
                weaponSocket.localPosition = new Vector3(0.35f, 0.2f, 0f);
                view.Initialize(
                    WeaponType.Dagger,
                    catalog.GetWeaponSprite(WeaponType.Dagger),
                    HelmetType.None,
                    null,
                    0,
                    0,
                    false);

                var weapon = FindRenderer(weaponSocket, "Weapon Layer");
                brokenPiece = weapon.gameObject;
                var worldPosition = brokenPiece.transform.position;
                var worldRotation = brokenPiece.transform.rotation;

                view.ApplyState(0, BrokenDefenseLayers.Weapon);

                Assert.That(view.HasWeaponVisual, Is.False);
                Assert.That(brokenPiece.transform.parent, Is.Null);
                Assert.That(brokenPiece.name, Is.EqualTo("Weapon Layer (Broken)"));
                Assert.That(brokenPiece.transform.position, Is.EqualTo(worldPosition));
                Assert.That(brokenPiece.transform.rotation, Is.EqualTo(worldRotation));
            }
            finally
            {
                if (brokenPiece != null)
                {
                    Object.DestroyImmediate(brokenPiece);
                }

                Object.DestroyImmediate(root);
            }
        }

        [TestCase("component")]
        [TestCase("bodyRenderer")]
        [TestCase("helmetSocket")]
        [TestCase("weaponSocket")]
        public void SpawnValidationRejectsMissingPrefabAttachment(string missingReference)
        {
            var config = CreateSpawnConfig(
                WeaponType.None,
                HelmetType.None,
                false,
                out var enemy,
                out var prefab);

            try
            {
                var view = prefab.GetComponent<EnemyDefenseView>();
                if (missingReference == "component")
                {
                    Object.DestroyImmediate(view);
                }
                else
                {
                    Set(view, missingReference, null);
                }

                Assert.That(EnemySpawnConfigValidator.TryValidate(config, out var error), Is.False);
                Assert.That(error, Does.Contain("prefab"));
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void CatalogValidationRejectsMissingSpriteBindings()
        {
            var catalog = ScriptableObject.CreateInstance<EnemyEquipmentVisualConfig>();

            try
            {
                Assert.That(catalog.TryValidate(out var error), Is.False);
                Assert.That(error, Does.Contain("every weapon and helmet sprite"));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [TestCase((WeaponType)999, HelmetType.None, false)]
        [TestCase(WeaponType.None, (HelmetType)999, false)]
        [TestCase(WeaponType.None, HelmetType.None, true)]
        public void SpawnValidationRejectsInvalidLoadouts(
            WeaponType weaponType,
            HelmetType helmetType,
            bool enchanted)
        {
            var config = CreateSpawnConfig(weaponType, helmetType, enchanted, out var enemy, out var prefab);

            try
            {
                Assert.That(EnemySpawnConfigValidator.TryValidate(config, out _), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void SpawningCopiesWaveLoadoutIntoEntityAndView()
        {
            var config = CreateSpawnConfig(
                WeaponType.Axe,
                HelmetType.Wood,
                true,
                out var enemy,
                out var prefab);
            var world = World.Create();
            var factory = new EnemyFactory(world);

            try
            {
                world.Create(new PlayerTag(), new Position { Value = Vector2.zero });
                var system = new SpawnEnemiesSystem(world, config, factory);
                system.Initialize();
                system.Update(new SystemState { Time = 0d, DeltaTime = 0.1f });

                var description = new QueryDescription()
                    .WithAll<EnemyDefense, GameObjectReference>();
                var count = 0;
                world.Query(in description, (
                    Entity _,
                    ref EnemyDefense defense,
                    ref GameObjectReference reference) =>
                {
                    count++;
                    Assert.That(defense.WeaponType, Is.EqualTo(WeaponType.Axe));
                    Assert.That(defense.HelmetType, Is.EqualTo(HelmetType.Wood));
                    Assert.That(defense.HelmetDurability, Is.EqualTo(3));
                    Assert.That(defense.IsEnchanted, Is.True);

                    var view = reference.GameObject.GetComponent<EnemyDefenseView>();
                    Assert.That(view, Is.Not.Null);
                    Assert.That(
                        FindRenderer(view.WeaponSocket, "Weapon Layer").sprite,
                        Is.SameAs(LoadCatalog().GetWeaponSprite(WeaponType.Axe)));
                    Assert.That(
                        FindRenderer(view.HelmetSocket, "Helmet Layer").sprite,
                        Is.SameAs(LoadCatalog().GetHelmetSprite(HelmetType.Wood)));
                });

                Assert.That(count, Is.EqualTo(1));
            }
            finally
            {
                factory.Dispose();
                world.Dispose();
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(enemy);
                Object.DestroyImmediate(config);
            }
        }

        private static EnemySpawnConfig CreateSpawnConfig(
            WeaponType weaponType,
            HelmetType helmetType,
            bool enchanted,
            out EnemyConfig enemy,
            out GameObject prefab)
        {
            var config = ScriptableObject.CreateInstance<EnemySpawnConfig>();
            enemy = ScriptableObject.CreateInstance<EnemyConfig>();
            prefab = new GameObject("Enemy Equipment Spawn Test Prefab");
            CreateConfiguredView(prefab, out _, out _, out _);
            Set(enemy, "prefab", prefab);
            Set(enemy, "baseSpeed", 1f);
            Set(enemy, "colliderRadius", 0.5f);
            Set(enemy, "statNoisePercentage", 0f);

            var entry = new EnemyWaveEntry();
            Set(entry, "enemy", enemy);
            Set(entry, "count", 1);
            Set(entry, "spawnInterval", 1f);
            Set(entry, "weaponType", weaponType);
            Set(entry, "helmetType", helmetType);
            Set(entry, "enchantedArmor", enchanted);

            var zone = new SpawnZone();
            Set(zone, "shape", SpawnShape.Circle);
            Set(zone, "circleRadius", 1f);
            var wave = new EnemyWaveConfig();
            Set(wave, "duration", 1f);
            Set(wave, "spawnZone", zone);
            Set(wave, "composition", new List<EnemyWaveEntry> { entry });

            Set(config, "equipmentVisuals", LoadCatalog());
            Set(config, "waves", new List<EnemyWaveConfig> { wave });
            return config;
        }

        private static EnemyEquipmentVisualConfig LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyEquipmentVisualConfig>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, $"Missing equipment catalog at {CatalogPath}.");
            return catalog;
        }

        private static EnemyDefenseView CreateConfiguredView(
            GameObject root,
            out SpriteRenderer bodyRenderer,
            out Transform helmetSocket,
            out Transform weaponSocket)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            bodyRenderer = visual.AddComponent<SpriteRenderer>();

            helmetSocket = new GameObject("HelmetSocket").transform;
            helmetSocket.SetParent(visual.transform, false);
            weaponSocket = new GameObject("WeaponSocket").transform;
            weaponSocket.SetParent(visual.transform, false);

            var view = root.AddComponent<EnemyDefenseView>();
            Set(view, "bodyRenderer", bodyRenderer);
            Set(view, "helmetSocket", helmetSocket);
            Set(view, "weaponSocket", weaponSocket);
            return view;
        }

        private static SpriteRenderer FindRenderer(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            Assert.That(child, Is.Not.Null, $"Missing child '{childName}'.");
            return child.GetComponent<SpriteRenderer>();
        }

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f));
        }

        private static void Set(object target, string fieldName, object value) =>
            target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
