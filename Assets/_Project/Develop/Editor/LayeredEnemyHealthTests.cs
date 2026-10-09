using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Systems;

namespace _Project.Editor.Tests
{
    public sealed class LayeredEnemyHealthTests
    {
        [TestCase(HelmetType.None, false, 0)]
        [TestCase(HelmetType.None, true, 0)]
        [TestCase(HelmetType.Cloth, false, 1)]
        [TestCase(HelmetType.Wood, false, 2)]
        [TestCase(HelmetType.Iron, false, 3)]
        [TestCase(HelmetType.DragonScale, false, 4)]
        [TestCase(HelmetType.Cloth, true, 2)]
        [TestCase(HelmetType.DragonScale, true, 5)]
        public void HelmetDurabilityMatchesTypeAndEnchantment(
            HelmetType helmetType,
            bool isEnchanted,
            int expected)
        {
            Assert.That(
                EnemyDefenseRules.GetHelmetDurability(helmetType, isEnchanted),
                Is.EqualTo(expected));
        }

        [TestCase(4, 4, HelmetVisualState.FullDurability)]
        [TestCase(3, 4, HelmetVisualState.Damaged)]
        [TestCase(2, 4, HelmetVisualState.Damaged)]
        [TestCase(1, 4, HelmetVisualState.OneHitRemaining)]
        [TestCase(0, 4, HelmetVisualState.None)]
        public void HelmetUsesAtMostThreeVisibleDurabilityStates(
            int current,
            int maximum,
            HelmetVisualState expected)
        {
            Assert.That(
                EnemyDefenseRules.GetHelmetVisualState(current, maximum),
                Is.EqualTo(expected));
        }

        [Test]
        public void UnequippedEnemyIsEliminatedByOneHit()
        {
            var defense = new EnemyDefense();

            var result = EnemyDefenseRules.ApplyHits(ref defense, 1);

            Assert.That(result.IsDefeated, Is.True);
            Assert.That(
                result.BrokenLayers,
                Is.EqualTo(BrokenDefenseLayers.Body));
        }

        [Test]
        public void ArmedEnemyLosesWeaponBeforeBody()
        {
            var defense = new EnemyDefense { HasWeapon = true };

            var firstHit = EnemyDefenseRules.ApplyHits(ref defense, 1);
            var secondHit = EnemyDefenseRules.ApplyHits(ref defense, 1);

            Assert.That(firstHit.IsDefeated, Is.False);
            Assert.That(firstHit.BrokenLayers, Is.EqualTo(BrokenDefenseLayers.Weapon));
            Assert.That(defense.HasWeapon, Is.False);
            Assert.That(secondHit.IsDefeated, Is.True);
            Assert.That(secondHit.BrokenLayers, Is.EqualTo(BrokenDefenseLayers.Body));
        }

        [Test]
        public void HelmetWeaponAndBodyAreRemovedInOrder()
        {
            var defense = new EnemyDefense
            {
                HelmetType = HelmetType.Iron,
                HelmetDurability = 3,
                MaxHelmetDurability = 3,
                HasWeapon = true
            };

            var helmetDamage = EnemyDefenseRules.ApplyHits(ref defense, 2);
            Assert.That(helmetDamage.IsDefeated, Is.False);
            Assert.That(helmetDamage.BrokenLayers, Is.EqualTo(BrokenDefenseLayers.None));
            Assert.That(defense.HelmetDurability, Is.EqualTo(1));

            var helmetBreak = EnemyDefenseRules.ApplyHits(ref defense, 1);
            Assert.That(helmetBreak.BrokenLayers, Is.EqualTo(BrokenDefenseLayers.Helmet));
            Assert.That(defense.HelmetDurability, Is.Zero);
            Assert.That(defense.HasWeapon, Is.True);

            var weaponBreak = EnemyDefenseRules.ApplyHits(ref defense, 1);
            Assert.That(weaponBreak.BrokenLayers, Is.EqualTo(BrokenDefenseLayers.Weapon));
            Assert.That(weaponBreak.IsDefeated, Is.False);

            var bodyBreak = EnemyDefenseRules.ApplyHits(ref defense, 1);
            Assert.That(bodyBreak.BrokenLayers, Is.EqualTo(BrokenDefenseLayers.Body));
            Assert.That(bodyBreak.IsDefeated, Is.True);
        }

        [Test]
        public void CollisionAgainstLayeredEnemyCreatesHitInsteadOfDamage()
        {
            var world = World.Create();

            try
            {
                var source = world.Create(new DamageOnCollision
                {
                    Amount = 100f,
                    Phase = CollisionPhase.Enter
                });
                var target = world.Create(new EnemyDefense());
                world.Create(new CollisionEvent
                {
                    First = source,
                    Second = target,
                    Phase = CollisionPhase.Enter
                });

                var system = new ApplyCollisionDamageSystem(world);
                system.Update(new SystemState());

                Assert.That(world.Has<HitRequest>(target), Is.True);
                Assert.That(world.Get<HitRequest>(target).Count, Is.EqualTo(1));
                Assert.That(world.Has<DamageRequest>(target), Is.False);
            }
            finally
            {
                world.Dispose();
            }
        }

        [Test]
        public void BatchedHitsTraverseEveryRemainingLayer()
        {
            var world = World.Create();

            try
            {
                var entity = world.Create(
                    new EnemyDefense
                    {
                        HelmetType = HelmetType.Iron,
                        HelmetDurability = 4,
                        MaxHelmetDurability = 4,
                        HasWeapon = true,
                        IsEnchanted = true
                    },
                    new HitRequest { Count = 6 });
                var system = new ApplyEnemyHitsSystem(world);

                system.Update(new SystemState());

                Assert.That(world.Get<EnemyDefense>(entity).HelmetDurability, Is.Zero);
                Assert.That(world.Get<EnemyDefense>(entity).HasWeapon, Is.False);
                Assert.That(world.Has<HitRequest>(entity), Is.False);
                Assert.That(world.Has<DeadTag>(entity), Is.True);
            }
            finally
            {
                world.Dispose();
            }
        }
    }
}