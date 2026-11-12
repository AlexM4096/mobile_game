using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Progression;
using _Project.Gameplay.Features.Progression.Systems;
using _Project.Gameplay.Features.Spear;
using _Project.Gameplay.Features.Sword;
using _Project.Gameplay.Features.LightningCharge;
using _Project.Gameplay.Features.RainOfArrows;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace _Project.Editor.Tests
{
    public sealed class ProgressionTests
    {
        private const string ProgressionConfigPath =
            "Assets/_Project/AddressablesResources/Progression/Configs/ProgressionConfig.asset";
        private const string SwordConfigPath =
            "Assets/_Project/AddressablesResources/Sword/Configs/SwordConfig.asset";
        private const string SpearConfigPath =
            "Assets/_Project/AddressablesResources/Spear/Configs/SpearConfig.asset";
        private const string LightningChargeConfigPath =
            "Assets/_Project/AddressablesResources/LightningCharge/Configs/LightningChargeConfig.asset";
        private const string RainOfArrowsConfigPath =
            "Assets/_Project/AddressablesResources/RainOfArrows/Configs/RainOfArrowsConfig.asset";

        private World _world;
        private ProgressionConfig _config;
        private SpearConfig _spearConfig;
        private LightningChargeConfig _lightningChargeConfig;
        private RainOfArrowsConfig _rainOfArrowsConfig;
        private GameObject _gridObject;
        private Grid _environmentGrid;
        private IUpgradeAvailabilityService _availability;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _config = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(ProgressionConfigPath);
            _spearConfig = AssetDatabase.LoadAssetAtPath<SpearConfig>(SpearConfigPath);
            _lightningChargeConfig =
                AssetDatabase.LoadAssetAtPath<LightningChargeConfig>(LightningChargeConfigPath);
            _rainOfArrowsConfig =
                AssetDatabase.LoadAssetAtPath<RainOfArrowsConfig>(RainOfArrowsConfigPath);
            _gridObject = new GameObject("Progression Test Grid");
            _environmentGrid = _gridObject.AddComponent<Grid>();
            Assert.That(_config, Is.Not.Null);
            Assert.That(_spearConfig, Is.Not.Null);
            Assert.That(_lightningChargeConfig, Is.Not.Null);
            Assert.That(_rainOfArrowsConfig, Is.Not.Null);
            Assert.That(_config.TryValidate(out var error), Is.True, error);
            _availability = new UpgradeAvailabilityService(_config, _spearConfig);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.DestroyImmediate(_gridObject);
        }

        [Test]
        public void ExperienceOverflowQueuesEveryCrossedLevel()
        {
            var player = _world.Create(new CharacterProgress
            {
                Level = 1,
                Experience = 30,
                RequiredExperience = 5
            });
            var system = new ResolveLevelUpsSystem(_world, _config, _availability);

            system.Update(default);

            var progress = _world.Get<CharacterProgress>(player);
            Assert.That(progress.Level, Is.EqualTo(4));
            Assert.That(progress.Experience, Is.Zero);
            Assert.That(progress.RequiredExperience, Is.EqualTo(20));
            Assert.That(progress.PendingSelections, Is.EqualTo(3));
        }

        [Test]
        public void ExperienceStopsAtMaximumLevel()
        {
            var player = _world.Create(new CharacterProgress
            {
                Level = 1,
                Experience = 10000,
                RequiredExperience = 5
            });
            var system = new ResolveLevelUpsSystem(_world, _config, _availability);

            system.Update(default);

            var progress = _world.Get<CharacterProgress>(player);
            Assert.That(progress.Level, Is.EqualTo(37));
            Assert.That(progress.PendingSelections, Is.EqualTo(36));
            Assert.That(progress.Experience, Is.EqualTo(progress.RequiredExperience));
        }

        [Test]
        public void DeadEnemyAwardsExperienceExactlyOnce()
        {
            var player = _world.Create(new CharacterProgress
            {
                Level = 1,
                RequiredExperience = 5
            });
            _world.Create(
                new DeadTag(),
                new ExperienceReward { Recipient = player, Amount = 3 });
            var queueSystem = new QueueEnemyExperienceSystem(_world);
            var applySystem = new ApplyExperienceSystem(_world, _availability);

            queueSystem.Update(default);
            applySystem.Update(default);
            queueSystem.Update(default);
            applySystem.Update(default);

            Assert.That(_world.Get<CharacterProgress>(player).Experience, Is.EqualTo(3));
        }

        [Test]
        public void OffersAreDeterministicUniqueAndRespectSpearPrerequisites()
        {
            var first = new List<UpgradeCardDefinition>();
            var second = new List<UpgradeCardDefinition>();
            var ranks = new UpgradeRanks();

            new UpgradeOfferGenerator(_config, _availability)
                .Generate(in ranks, false, false, false, first);
            new UpgradeOfferGenerator(_config, _availability)
                .Generate(in ranks, false, false, false, second);

            Assert.That(first, Has.Count.EqualTo(3));
            Assert.That(first[0].Id, Is.EqualTo(second[0].Id));
            Assert.That(first[1].Id, Is.EqualTo(second[1].Id));
            Assert.That(first[2].Id, Is.EqualTo(second[2].Id));
            Assert.That(new HashSet<UpgradeId>
            {
                first[0].Id,
                first[1].Id,
                first[2].Id
            }, Has.Count.EqualTo(3));
            Assert.That(first, Has.None.Matches<UpgradeCardDefinition>(
                card => UpgradeRules.RequiresSpear(card.Id) ||
                        UpgradeRules.RequiresLightningCharge(card.Id) ||
                        UpgradeRules.RequiresRainOfArrows(card.Id)));
        }

        [Test]
        public void OfferGeneratorReturnsAllRemainingCardsWhenFewerThanThreeRemain()
        {
            var ranks = new UpgradeRanks
            {
                Spear = 1,
                AdditionalBlade = 3,
                WhirlingBlades = 5,
                SplitThrow = 3,
                Quickdraw = 4,
                HeavySpear = 3,
                ReinforcedShaft = 2,
                LightningCharge = 1,
                Capacitor = 3,
                RainOfArrows = 1,
                SaturationFire = 3,
                WideningBarrage = 3,
                RapidVolley = 3
            };
            var offers = new List<UpgradeCardDefinition>();

            new UpgradeOfferGenerator(_config, _availability)
                .Generate(in ranks, true, true, true, offers);

            Assert.That(offers, Has.Count.EqualTo(1));
            Assert.That(offers[0].Id, Is.EqualTo(UpgradeId.ReinforcedShaft));
        }

        [Test]
        public void EveryConfiguredUpgradeChangesRuntimeAbilityState()
        {
            var swordConfig = AssetDatabase.LoadAssetAtPath<SwordConfig>(SwordConfigPath);
            var player = _world.Create(
                new Position(),
                new PlayerMotion(),
                new SwordAbility
                {
                    WeaponCount = 1,
                    BaseRotationSpeedDegreesPerSecond = 180f,
                    RotationSpeedMultiplier = 1f
                },
                new CharacterProgress
                {
                    Level = 14,
                    RequiredExperience = 40,
                    PendingSelections = 13
                },
                new UpgradeRanks());
            var system = new ApplySelectedUpgradeSystem(
                _world,
                _config,
                _spearConfig,
                _lightningChargeConfig,
                _rainOfArrowsConfig,
                _environmentGrid,
                _availability,
                new SwordFactory(_world, swordConfig));

            Apply(system, player, UpgradeId.Spear);
            Apply(system, player, UpgradeId.AdditionalBlade);
            Apply(system, player, UpgradeId.WhirlingBlades);
            Apply(system, player, UpgradeId.SplitThrow);
            Apply(system, player, UpgradeId.Quickdraw);
            Apply(system, player, UpgradeId.HeavySpear);
            Apply(system, player, UpgradeId.ReinforcedShaft);
            Apply(system, player, UpgradeId.LightningCharge);
            Apply(system, player, UpgradeId.Capacitor);
            Apply(system, player, UpgradeId.RainOfArrows);
            Apply(system, player, UpgradeId.SaturationFire);
            Apply(system, player, UpgradeId.WideningBarrage);
            Apply(system, player, UpgradeId.RapidVolley);

            var sword = _world.Get<SwordAbility>(player);
            var spear = _world.Get<SpearAbility>(player);
            var lightningCharge = _world.Get<LightningChargeAbility>(player);
            var rainOfArrows = _world.Get<RainOfArrowsAbility>(player);
            var ranks = _world.Get<UpgradeRanks>(player);
            Assert.That(sword.WeaponCount, Is.EqualTo(2));
            Assert.That(sword.RotationSpeedMultiplier, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(spear.ProjectileCount, Is.EqualTo(_spearConfig.ProjectileCount + 1));
            Assert.That(spear.CooldownDuration, Is.EqualTo(1.7f).Within(0.0001f));
            Assert.That(spear.DamageHits, Is.EqualTo(_spearConfig.DamageHits + 1));
            Assert.That(spear.TotalHealth, Is.EqualTo(_spearConfig.TotalHealth + 1));
            Assert.That(
                lightningCharge.ChargesPerActivation,
                Is.EqualTo(_lightningChargeConfig.BaseChargeCount + 1));
            Assert.That(
                lightningCharge.TransferRadius,
                Is.EqualTo(_lightningChargeConfig.GetTransferRadius(_environmentGrid))
                    .Within(0.0001f));
            Assert.That(
                rainOfArrows.ProjectileCount,
                Is.EqualTo(_rainOfArrowsConfig.ProjectileCount + 3));
            Assert.That(
                rainOfArrows.ZoneRadius,
                Is.EqualTo(
                    _rainOfArrowsConfig.GetZoneRadius(_environmentGrid) +
                    _rainOfArrowsConfig.GetTileSize(_environmentGrid) * 0.5f)
                    .Within(0.0001f));
            Assert.That(rainOfArrows.CooldownDuration, Is.EqualTo(7.2f).Within(0.0001f));
            Assert.That(ranks.Spear, Is.EqualTo(1));
            Assert.That(ranks.LightningCharge, Is.EqualTo(1));
            Assert.That(ranks.Capacitor, Is.EqualTo(1));
            Assert.That(ranks.RainOfArrows, Is.EqualTo(1));
            Assert.That(ranks.SaturationFire, Is.EqualTo(1));
            Assert.That(ranks.WideningBarrage, Is.EqualTo(1));
            Assert.That(ranks.RapidVolley, Is.EqualTo(1));
            Assert.That(_world.Get<CharacterProgress>(player).PendingSelections, Is.Zero);
        }

        [Test]
        public void RainOfArrowsUpgradeFamilyReachesConfiguredFinalValues()
        {
            var swordConfig = AssetDatabase.LoadAssetAtPath<SwordConfig>(SwordConfigPath);
            var player = _world.Create(
                new CharacterProgress
                {
                    Level = 11,
                    RequiredExperience = 55,
                    PendingSelections = 10
                },
                new UpgradeRanks());
            var system = new ApplySelectedUpgradeSystem(
                _world,
                _config,
                _spearConfig,
                _lightningChargeConfig,
                _rainOfArrowsConfig,
                _environmentGrid,
                _availability,
                new SwordFactory(_world, swordConfig));

            Apply(system, player, UpgradeId.RainOfArrows);
            for (var rank = 0; rank < 3; rank++)
            {
                Apply(system, player, UpgradeId.SaturationFire);
                Apply(system, player, UpgradeId.WideningBarrage);
                Apply(system, player, UpgradeId.RapidVolley);
            }

            var ability = _world.Get<RainOfArrowsAbility>(player);
            Assert.That(ability.ProjectileCount, Is.EqualTo(21));
            Assert.That(
                ability.ZoneRadius,
                Is.EqualTo(_rainOfArrowsConfig.GetTileSize(_environmentGrid) * 4.5f)
                    .Within(0.0001f));
            Assert.That(ability.CooldownDuration, Is.EqualTo(5.6f).Within(0.0001f));
            Assert.That(_world.Get<CharacterProgress>(player).PendingSelections, Is.Zero);
        }

        [Test]
        public void DisabledSpearHidesItsUpgradeFamilyAndCapsRunAtLevelTwentyThree()
        {
            var disabledSpear = CreateDisabledSpearConfig();
            try
            {
                var availability = new UpgradeAvailabilityService(_config, disabledSpear);
                Assert.That(
                    SwarmSpawner.TryValidateSpearConfiguration(
                        disabledSpear,
                        out var validationError),
                    Is.True,
                    validationError);
                Assert.That(availability.MaximumRunLevel, Is.EqualTo(23));
                Assert.That(availability.IsAvailable(UpgradeId.AdditionalBlade), Is.True);
                Assert.That(availability.IsAvailable(UpgradeId.WhirlingBlades), Is.True);
                Assert.That(availability.IsAvailable(UpgradeId.Spear), Is.False);
                Assert.That(availability.IsAvailable(UpgradeId.SplitThrow), Is.False);
                Assert.That(availability.IsAvailable(UpgradeId.Quickdraw), Is.False);
                Assert.That(availability.IsAvailable(UpgradeId.HeavySpear), Is.False);
                Assert.That(availability.IsAvailable(UpgradeId.ReinforcedShaft), Is.False);
                Assert.That(availability.IsAvailable(UpgradeId.LightningCharge), Is.True);
                Assert.That(availability.IsAvailable(UpgradeId.Capacitor), Is.True);

                var offers = new List<UpgradeCardDefinition>();
                var ranks = new UpgradeRanks();
                new UpgradeOfferGenerator(_config, availability)
                    .Generate(in ranks, false, false, false, offers);
                Assert.That(offers, Has.Count.EqualTo(3));
                Assert.That(offers, Has.None.Matches<UpgradeCardDefinition>(
                    card => UpgradeRules.RequiresSpear(card.Id) ||
                            UpgradeRules.RequiresLightningCharge(card.Id) ||
                            UpgradeRules.RequiresRainOfArrows(card.Id)));

                var player = _world.Create(new CharacterProgress
                {
                    Level = 1,
                    Experience = 10000,
                    RequiredExperience = 5
                });
                new ResolveLevelUpsSystem(_world, _config, availability).Update(default);
                var progress = _world.Get<CharacterProgress>(player);
                Assert.That(progress.Level, Is.EqualTo(23));
                Assert.That(progress.PendingSelections, Is.EqualTo(22));
                Assert.That(progress.Experience, Is.EqualTo(progress.RequiredExperience));

                _world.Create(new GrantExperienceRequest
                {
                    Target = player,
                    Amount = 100
                });
                new ApplyExperienceSystem(_world, availability).Update(default);
                Assert.That(
                    _world.Get<CharacterProgress>(player).Experience,
                    Is.EqualTo(progress.RequiredExperience));
            }
            finally
            {
                Object.DestroyImmediate(disabledSpear);
            }
        }

        [Test]
        public void DisabledSpearRejectsDirectUnlockRequest()
        {
            var disabledSpear = CreateDisabledSpearConfig();
            try
            {
                var availability = new UpgradeAvailabilityService(_config, disabledSpear);
                var swordConfig = AssetDatabase.LoadAssetAtPath<SwordConfig>(SwordConfigPath);
                var player = _world.Create(
                    new CharacterProgress
                    {
                        Level = 2,
                        RequiredExperience = 10,
                        PendingSelections = 1
                    },
                    new UpgradeRanks());
                var system = new ApplySelectedUpgradeSystem(
                    _world,
                    _config,
                    disabledSpear,
                    _lightningChargeConfig,
                    _rainOfArrowsConfig,
                    _environmentGrid,
                    availability,
                    new SwordFactory(_world, swordConfig));

                LogAssert.Expect(LogType.Error, "Upgrade 'Spear' is not currently eligible.");
                Apply(system, player, UpgradeId.Spear);

                Assert.That(_world.Has<SpearAbility>(player), Is.False);
                Assert.That(_world.Get<UpgradeRanks>(player).Spear, Is.Zero);
                Assert.That(
                    _world.Get<CharacterProgress>(player).PendingSelections,
                    Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(disabledSpear);
            }
        }

        private static SpearConfig CreateDisabledSpearConfig()
        {
            var config = ScriptableObject.CreateInstance<SpearConfig>();
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("enabled").boolValue = false;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private void Apply(
            ApplySelectedUpgradeSystem system,
            Entity player,
            UpgradeId upgrade)
        {
            _world.Create(new ApplyUpgradeRequest { Target = player, Upgrade = upgrade });
            system.Update(new SystemState { Time = 10d });
        }
    }
}
