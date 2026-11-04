using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Spear;
using _Project.Gameplay.Features.Sword;
using _Project.Gameplay.Features.LightningCharge;
using UnityEngine;

namespace _Project.Gameplay.Features.Progression.Systems
{
    public sealed class ApplySelectedUpgradeSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<ApplyUpgradeRequest>();

        private readonly ProgressionConfig _config;
        private readonly SpearConfig _spearConfig;
        private readonly LightningChargeConfig _lightningChargeConfig;
        private readonly Grid _environmentGrid;
        private readonly IUpgradeAvailabilityService _availability;
        private readonly SwordFactory _swordFactory;
        private readonly List<Entity> _requestEntities = new();
        private readonly List<ApplyUpgradeRequest> _requests = new();

        public ApplySelectedUpgradeSystem(
            World world,
            ProgressionConfig config,
            SpearConfig spearConfig,
            LightningChargeConfig lightningChargeConfig,
            Grid environmentGrid,
            IUpgradeAvailabilityService availability,
            SwordFactory swordFactory) : base(world)
        {
            _config = config;
            _spearConfig = spearConfig;
            _lightningChargeConfig = lightningChargeConfig;
            _environmentGrid = environmentGrid;
            _availability = availability;
            _swordFactory = swordFactory;
        }

        public override void Update(in SystemState state)
        {
            _requestEntities.Clear();
            _requests.Clear();
            World.Query(in _description, (Entity entity, ref ApplyUpgradeRequest request) =>
            {
                _requestEntities.Add(entity);
                _requests.Add(request);
            });

            for (var index = 0; index < _requests.Count; index++)
            {
                Apply(_requests[index], state.Time);
                if (World.IsAlive(_requestEntities[index]))
                {
                    World.Destroy(_requestEntities[index]);
                }
            }
        }

        private void Apply(in ApplyUpgradeRequest request, double currentTime)
        {
            if (!World.IsAlive(request.Target) ||
                !World.Has<CharacterProgress>(request.Target) ||
                !World.Has<UpgradeRanks>(request.Target))
            {
                return;
            }

            var card = _config.GetCard(request.Upgrade);
            if (card == null)
            {
                Debug.LogError($"No configured progression card exists for '{request.Upgrade}'.");
                return;
            }

            ref var progress = ref World.Get<CharacterProgress>(request.Target);
            ref var ranks = ref World.Get<UpgradeRanks>(request.Target);
            var currentRank = UpgradeRules.GetRank(in ranks, request.Upgrade);
            var hasSpear = World.Has<SpearAbility>(request.Target);
            var hasLightningCharge = World.Has<LightningChargeAbility>(request.Target);
            if (!_availability.IsAvailable(request.Upgrade) ||
                progress.PendingSelections <= 0 ||
                currentRank >= card.MaximumRank ||
                (request.Upgrade == UpgradeId.Spear && hasSpear) ||
                (UpgradeRules.RequiresSpear(request.Upgrade) && !hasSpear) ||
                (request.Upgrade == UpgradeId.LightningCharge && hasLightningCharge) ||
                (UpgradeRules.RequiresLightningCharge(request.Upgrade) && !hasLightningCharge))
            {
                Debug.LogError($"Upgrade '{request.Upgrade}' is not currently eligible.");
                return;
            }

            switch (request.Upgrade)
            {
                case UpgradeId.Spear:
                    World.Add(request.Target, new SpearAbility
                    {
                        NextActivationTime = currentTime,
                        CooldownDuration = _spearConfig.CooldownDuration,
                        ProjectileCount = _spearConfig.ProjectileCount,
                        DamageHits = _spearConfig.DamageHits,
                        TotalHealth = _spearConfig.TotalHealth
                    });
                    break;
                case UpgradeId.AdditionalBlade:
                {
                    ref var ability = ref World.Get<SwordAbility>(request.Target);
                    var firstSlotIndex = ability.WeaponCount;
                    var addedCount = Mathf.Max(1, Mathf.RoundToInt(card.Magnitude));
                    ability.WeaponCount += addedCount;
                    for (var slotOffset = 0; slotOffset < addedCount; slotOffset++)
                    {
                        _swordFactory.CreateAdditionalFor(
                            request.Target,
                            firstSlotIndex + slotOffset);
                    }
                    break;
                }
                case UpgradeId.WhirlingBlades:
                {
                    ref var ability = ref World.Get<SwordAbility>(request.Target);
                    ability.RotationSpeedMultiplier += card.Magnitude;
                    break;
                }
                case UpgradeId.SplitThrow:
                {
                    ref var ability = ref World.Get<SpearAbility>(request.Target);
                    ability.ProjectileCount += Mathf.RoundToInt(card.Magnitude);
                    break;
                }
                case UpgradeId.Quickdraw:
                {
                    ref var ability = ref World.Get<SpearAbility>(request.Target);
                    ability.CooldownDuration = Mathf.Max(
                        0.01f,
                        _spearConfig.CooldownDuration *
                        (1f - card.Magnitude * (currentRank + 1)));
                    break;
                }
                case UpgradeId.HeavySpear:
                {
                    ref var ability = ref World.Get<SpearAbility>(request.Target);
                    ability.DamageHits += Mathf.RoundToInt(card.Magnitude);
                    break;
                }
                case UpgradeId.ReinforcedShaft:
                {
                    ref var ability = ref World.Get<SpearAbility>(request.Target);
                    ability.TotalHealth += Mathf.RoundToInt(card.Magnitude);
                    break;
                }
                case UpgradeId.LightningCharge:
                    World.Add(request.Target, new LightningChargeAbility
                    {
                        NextActivationTime = currentTime,
                        CooldownDuration = _lightningChargeConfig.CooldownDuration,
                        ChargeDuration = _lightningChargeConfig.ChargeDuration,
                        TransferRadius = _lightningChargeConfig.GetTransferRadius(_environmentGrid),
                        ChargesPerActivation = _lightningChargeConfig.BaseChargeCount,
                        DamageHits = _lightningChargeConfig.DamageHits,
                        MaxTargetsPerChain = _lightningChargeConfig.MaxTargetsPerChain
                    });
                    break;
                case UpgradeId.Capacitor:
                {
                    ref var ability = ref World.Get<LightningChargeAbility>(request.Target);
                    ability.ChargesPerActivation += Mathf.Max(1, Mathf.RoundToInt(card.Magnitude));
                    break;
                }
            }

            ref var updatedRanks = ref World.Get<UpgradeRanks>(request.Target);
            ref var updatedProgress = ref World.Get<CharacterProgress>(request.Target);
            UpgradeRules.SetRank(ref updatedRanks, request.Upgrade, currentRank + 1);
            updatedProgress.PendingSelections--;
        }
    }
}
