using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Systems;
using _Project.Gameplay.Features.LightningCharge;
using _Project.Gameplay.Features.LightningCharge.Systems;
using _Project.Gameplay.Features.Sword;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class LightningChargeTests
    {
        private World _world;
        private GameObject _gridObject;
        private LightningChargeConfig _config;
        private LightningTargetRegistry _targets;
        private LightningArcFactory _arcFactory;
        private CollectLightningChargeTargetsSystem _collectTargetsSystem;
        private ApplyLightningChargeHitsSystem _applyHitsSystem;
        private ApplyEnemyHitsSystem _applyEnemyHitsSystem;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _gridObject = new GameObject("Lightning Charge Test Grid");
            _gridObject.AddComponent<Grid>();
            _config = ScriptableObject.CreateInstance<LightningChargeConfig>();
            _targets = new LightningTargetRegistry();
            _arcFactory = new LightningArcFactory(_world, _config);
            _collectTargetsSystem = new CollectLightningChargeTargetsSystem(_world, _targets);
            _applyHitsSystem = new ApplyLightningChargeHitsSystem(
                _world,
                _targets,
                _arcFactory);
            _applyEnemyHitsSystem = new ApplyEnemyHitsSystem(_world);
        }

        [TearDown]
        public void TearDown()
        {
            _arcFactory.Dispose();
            _world.Dispose();
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_gridObject);
        }

        [Test]
        public void ActivationDoesNotOverwriteActiveBatchAndExpiredBatchReactivatesWhenReady()
        {
            var owner = _world.Create(CreateAbility(chargesPerActivation: 2));
            var system = new UpdateLightningChargeStateSystem(_world);

            system.Update(new SystemState { Time = 10d });
            AssertAbility(owner, 2, 15d, 16d);

            ref var ability = ref _world.Get<LightningChargeAbility>(owner);
            ability.RemainingCharges = 1;
            ability.ChargeExpiresAt = 17d;
            system.Update(new SystemState { Time = 16d });
            AssertAbility(owner, 1, 17d, 16d);

            system.Update(new SystemState { Time = 17d });
            AssertAbility(owner, 2, 22d, 23d);
        }

        [Test]
        public void ExpiryDiscardsUnusedChargesBeforeCooldownIsReady()
        {
            var ability = CreateAbility(chargesPerActivation: 4);
            ability.NextActivationTime = 20d;
            ability.RemainingCharges = 3;
            ability.ChargeExpiresAt = 15d;
            var owner = _world.Create(ability);

            new UpdateLightningChargeStateSystem(_world)
                .Update(new SystemState { Time = 15d });

            AssertAbility(owner, 0, 0d, 20d);
        }

        [Test]
        public void ContactConsumesSharedChargeAndRefreshesNextChargeDuration()
        {
            var owner = CreateOwner(CreateChargedAbility(2, chargeExpiresAt: 12d));
            var firstSword = CreateSword(owner, 0, Vector2.zero);
            var secondSword = CreateSword(owner, 1, Vector2.right * 2f);
            var firstEnemy = CreateEnemy(Vector2.zero, 5);
            var secondEnemy = CreateEnemy(Vector2.right * 2f, 5);
            CollectTargets();

            CreateCollision(firstSword, firstEnemy, CollisionPhase.Enter);
            CreateCollision(secondSword, secondEnemy, CollisionPhase.Enter);
            _applyHitsSystem.Update(new SystemState { Time = 10d });
            _applyEnemyHitsSystem.Update(default);

            ref var ability = ref _world.Get<LightningChargeAbility>(owner);
            Assert.That(ability.RemainingCharges, Is.Zero);
            Assert.That(ability.ChargeExpiresAt, Is.Zero);
            AssertDurability(firstEnemy, 4);
            AssertDurability(secondEnemy, 4);
        }

        [Test]
        public void ChainSelectsNearestUniqueTargetsAndStopsAtConfiguredCount()
        {
            var owner = CreateOwner(CreateChargedAbility(1, maxTargets: 3, radius: 1.1f));
            var sword = CreateSword(owner, 0, Vector2.zero);
            var initial = CreateEnemy(Vector2.zero, 5);
            var nearest = CreateEnemy(Vector2.right, 5);
            var chainedFromNearest = CreateEnemy(Vector2.right * 2f, 5);
            var excludedByCount = CreateEnemy(Vector2.right * 3f, 5);
            CollectTargets();

            CreateCollision(sword, initial, CollisionPhase.Enter);
            _applyHitsSystem.Update(new SystemState { Time = 10d });
            _applyEnemyHitsSystem.Update(default);

            AssertDurability(initial, 4);
            AssertDurability(nearest, 4);
            AssertDurability(chainedFromNearest, 4);
            AssertDurability(excludedByCount, 5);
        }

        [Test]
        public void ChainUsesEntityIdentityToBreakEqualDistanceTies()
        {
            var owner = CreateOwner(CreateChargedAbility(1, maxTargets: 2, radius: 2f));
            var sword = CreateSword(owner, 0, Vector2.zero);
            var initial = CreateEnemy(Vector2.zero, 5);
            var firstTie = CreateEnemy(Vector2.left, 5);
            var secondTie = CreateEnemy(Vector2.right, 5);
            var expected = firstTie.CompareTo(secondTie) < 0 ? firstTie : secondTie;
            var other = expected.Equals(firstTie) ? secondTie : firstTie;
            CollectTargets();

            CreateCollision(sword, initial, CollisionPhase.Enter);
            _applyHitsSystem.Update(new SystemState { Time = 10d });
            _applyEnemyHitsSystem.Update(default);

            AssertDurability(expected, 4);
            AssertDurability(other, 5);
        }

        [Test]
        public void ChainStopsWhenNoUnhitEnemyIsInsideTransferRadius()
        {
            var owner = CreateOwner(CreateChargedAbility(1, maxTargets: 4, radius: 1.1f));
            var sword = CreateSword(owner, 0, Vector2.zero);
            var initial = CreateEnemy(Vector2.zero, 5);
            var inRange = CreateEnemy(Vector2.right, 5);
            var outOfRange = CreateEnemy(Vector2.right * 3f, 5);
            CollectTargets();

            CreateCollision(sword, initial, CollisionPhase.Enter);
            _applyHitsSystem.Update(new SystemState { Time = 10d });
            _applyEnemyHitsSystem.Update(default);

            AssertDurability(initial, 4);
            AssertDurability(inRange, 4);
            AssertDurability(outOfRange, 5);
        }

        [Test]
        public void StayAndUnchargedContactsDoNotTriggerLightning()
        {
            var owner = CreateOwner(CreateChargedAbility(1));
            var sword = CreateSword(owner, 0, Vector2.zero);
            var enemy = CreateEnemy(Vector2.zero, 5);
            CollectTargets();

            CreateCollision(sword, enemy, CollisionPhase.Stay);
            _applyHitsSystem.Update(new SystemState { Time = 10d });
            Assert.That(_world.Has<HitRequest>(enemy), Is.False);

            ref var ability = ref _world.Get<LightningChargeAbility>(owner);
            ability.RemainingCharges = 0;
            CreateCollision(sword, enemy, CollisionPhase.Enter);
            _applyHitsSystem.Update(new SystemState { Time = 10d });
            Assert.That(_world.Has<HitRequest>(enemy), Is.False);
        }

        [Test]
        public void FatalCombinedSwordAndLightningHitStillChains()
        {
            var owner = CreateOwner(CreateChargedAbility(1, maxTargets: 2, radius: 3f));
            var sword = CreateSword(owner, 0, Vector2.zero, addCollisionDamage: true);
            var initial = CreateEnemy(Vector2.zero, 0);
            var chained = CreateEnemy(Vector2.right * 2f, 2);
            CollectTargets();
            CreateCollision(sword, initial, CollisionPhase.Enter);

            _applyHitsSystem.Update(new SystemState { Time = 10d });
            new ApplyCollisionDamageSystem(_world).Update(default);
            _applyEnemyHitsSystem.Update(default);

            Assert.That(_world.Has<DeadTag>(initial), Is.True);
            AssertDurability(chained, 1);
        }

        [Test]
        public void ChargeViewRestoresOriginalSpriteColor()
        {
            var root = new GameObject("Lightning Charge View Test");
            try
            {
                var child = new GameObject("Visual");
                child.transform.SetParent(root.transform);
                var renderer = child.AddComponent<SpriteRenderer>();
                var original = new Color(0.7f, 0.6f, 0.5f, 1f);
                var charged = new Color(0.2f, 0.9f, 1f, 1f);
                renderer.color = original;
                var view = root.AddComponent<LightningChargeView>();

                view.SetCharged(true, charged);
                Assert.That(renderer.color, Is.EqualTo(charged));

                view.SetCharged(false, charged);
                Assert.That(renderer.color, Is.EqualTo(original));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ViewSystemTintsEveryOwnedSwordAndRestoresThemWhenUncharged()
        {
            var owner = CreateOwner(CreateChargedAbility(1));
            var firstView = CreateSwordView("First Sword", Color.white, out var firstRenderer);
            var secondOriginal = new Color(0.8f, 0.7f, 0.6f, 1f);
            var secondView = CreateSwordView("Second Sword", secondOriginal, out var secondRenderer);
            try
            {
                var firstSword = CreateSword(owner, 0, Vector2.zero);
                var secondSword = CreateSword(owner, 1, Vector2.right);
                _world.Add(firstSword, new GameObjectReference(firstView));
                _world.Add(secondSword, new GameObjectReference(secondView));
                var system = new UpdateLightningChargeSwordViewsSystem(_world, _config);

                system.Update(new SystemState { Time = 10d });
                Assert.That(firstRenderer.color, Is.EqualTo(_config.ChargedColor));
                Assert.That(secondRenderer.color, Is.EqualTo(_config.ChargedColor));

                ref var ability = ref _world.Get<LightningChargeAbility>(owner);
                ability.RemainingCharges = 0;
                system.Update(new SystemState { Time = 10d });
                Assert.That(firstRenderer.color, Is.EqualTo(Color.white));
                Assert.That(secondRenderer.color, Is.EqualTo(secondOriginal));
            }
            finally
            {
                Object.DestroyImmediate(firstView);
                Object.DestroyImmediate(secondView);
            }
        }

        private LightningChargeAbility CreateAbility(int chargesPerActivation) =>
            new()
            {
                NextActivationTime = 10d,
                CooldownDuration = 6f,
                ChargeDuration = 5f,
                TransferRadius = 3f,
                ChargesPerActivation = chargesPerActivation,
                DamageHits = 1,
                MaxTargetsPerChain = 3
            };

        private LightningChargeAbility CreateChargedAbility(
            int remainingCharges,
            double chargeExpiresAt = 15d,
            int maxTargets = 1,
            float radius = 3f)
        {
            var ability = CreateAbility(remainingCharges);
            ability.NextActivationTime = 16d;
            ability.RemainingCharges = remainingCharges;
            ability.ChargeExpiresAt = chargeExpiresAt;
            ability.MaxTargetsPerChain = maxTargets;
            ability.TransferRadius = radius;
            return ability;
        }

        private Entity CreateOwner(LightningChargeAbility ability) =>
            _world.Create(ability);

        private Entity CreateSword(
            Entity owner,
            int slotIndex,
            Vector2 position,
            bool addCollisionDamage = false)
        {
            var sword = _world.Create(
                new SwordTag(),
                new SwordOrbit { Owner = owner, SlotIndex = slotIndex },
                new Position { Value = position });
            if (addCollisionDamage)
            {
                _world.Add(sword, new DamageOnCollision
                {
                    Amount = 1f,
                    Phase = CollisionPhase.Enter
                });
            }

            return sword;
        }

        private Entity CreateEnemy(Vector2 position, int helmetDurability)
        {
            return _world.Create(
                new Position { Value = position },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Enemy
                },
                new EnemyDefense
                {
                    HelmetType = helmetDurability > 0 ? HelmetType.DragonScale : HelmetType.None,
                    HelmetDurability = helmetDurability,
                    MaxHelmetDurability = helmetDurability
                });
        }

        private static GameObject CreateSwordView(
            string name,
            Color originalColor,
            out SpriteRenderer spriteRenderer)
        {
            var root = new GameObject(name);
            var child = new GameObject("Visual");
            child.transform.SetParent(root.transform);
            spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.color = originalColor;
            root.AddComponent<LightningChargeView>();
            return root;
        }

        private void CollectTargets()
        {
            _collectTargetsSystem.Update(default);
        }

        private void CreateCollision(Entity sword, Entity enemy, CollisionPhase phase)
        {
            _world.Create(new CollisionEvent
            {
                First = sword,
                Second = enemy,
                Phase = phase
            });
        }

        private void AssertAbility(
            Entity owner,
            int remainingCharges,
            double chargeExpiresAt,
            double nextActivationTime)
        {
            var ability = _world.Get<LightningChargeAbility>(owner);
            Assert.That(ability.RemainingCharges, Is.EqualTo(remainingCharges));
            Assert.That(ability.ChargeExpiresAt, Is.EqualTo(chargeExpiresAt).Within(0.0001d));
            Assert.That(ability.NextActivationTime, Is.EqualTo(nextActivationTime).Within(0.0001d));
        }

        private void AssertDurability(Entity enemy, int expected)
        {
            Assert.That(_world.Get<EnemyDefense>(enemy).HelmetDurability, Is.EqualTo(expected));
        }
    }
}
