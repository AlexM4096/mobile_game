using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Progression;
using PrimeTween;
using UnityEngine;
using Random = System.Random;

namespace _Project.Gameplay.Features.EnemySpawn.Systems
{
    public sealed class SplitDeadEnemiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<
                    DeadTag,
                    SplitOnDeath,
                    Position,
                    CircleCollider,
                    ExperienceReward,
                    MoveSpeed,
                    GameObjectReference>()
                .WithNone<SplitProcessedTag>();

        private readonly EnemySpawnConfig _spawnConfig;
        private readonly EnemyFactory _factory;
        private readonly List<PendingSplit> _pendingSplits = new();
        private readonly Random _random;

        public SplitDeadEnemiesSystem(
            World world,
            EnemySpawnConfig spawnConfig,
            EnemyFactory factory) : base(world)
        {
            _spawnConfig = spawnConfig;
            _factory = factory;
            _random = new Random(unchecked(spawnConfig.RandomSeed ^ (int)0x9E3779B9));
        }

        public override void Update(in SystemState state)
        {
            _pendingSplits.Clear();
            var currentTime = state.Time;
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (
                Entity entity,
                ref SplitOnDeath split,
                ref Position position,
                ref CircleCollider collider,
                ref ExperienceReward reward,
                ref MoveSpeed moveSpeed,
                ref GameObjectReference viewReference) =>
            {
                if (!split.AnimationStarted)
                {
                    split.AnimationStarted = true;
                    split.SpawnAt = currentTime + split.Delay;
                    moveSpeed.Value = 0f;
                    commandBuffer.Remove<CollisionBody>(entity);
                    PlaySplitAnimation(viewReference.GameObject, split.Delay);
                    return;
                }

                if (currentTime < split.SpawnAt)
                {
                    return;
                }

                _pendingSplits.Add(new PendingSplit(
                    split,
                    position.Value,
                    collider.Radius,
                    reward.Recipient));
                commandBuffer.Add<SplitProcessedTag>(entity);
                commandBuffer.Add<DestroySelfRequest>(entity);
            });

            commandBuffer.Playback(World, false);

            for (var splitIndex = 0; splitIndex < _pendingSplits.Count; splitIndex++)
            {
                SpawnChildren(_pendingSplits[splitIndex]);
            }
        }

        private static void PlaySplitAnimation(GameObject view, float duration)
        {
            if (view == null)
            {
                return;
            }

            Tween.Scale(
                view.transform,
                Vector3.zero,
                duration,
                Ease.InBack);
        }

        private void SpawnChildren(PendingSplit pending)
        {
            if (!_factory.TryGetConfig(pending.Split.ChildConfigId, out var childConfig))
            {
                Debug.LogError(
                    $"Cannot split enemy because child config id " +
                    $"{pending.Split.ChildConfigId} is not registered.");
                return;
            }

            var loadout = new EnemyEquipmentLoadout(
                pending.Split.WeaponType,
                pending.Split.HelmetType,
                pending.Split.IsEnchanted);
            var radius = GetRingRadius(
                pending.Split.Count,
                pending.ParentRadius,
                childConfig.ColliderRadius);

            for (var childIndex = 0; childIndex < pending.Split.Count; childIndex++)
            {
                var childPosition = GetChildPosition(
                    pending.Position,
                    radius,
                    childIndex,
                    pending.Split.Count);
                _factory.Create(
                    childConfig,
                    loadout,
                    _spawnConfig.EquipmentVisuals,
                    childPosition,
                    _random,
                    pending.ExperienceRecipient);
            }
        }

        private static float GetRingRadius(
            int count,
            float parentRadius,
            float childRadius)
        {
            if (count <= 1)
            {
                return 0f;
            }

            var minimumChildSpacingRadius =
                childRadius / Mathf.Sin(Mathf.PI / count);
            return Mathf.Max(parentRadius, minimumChildSpacingRadius);
        }

        private static Vector2 GetChildPosition(
            Vector2 center,
            float radius,
            int index,
            int count)
        {
            if (count <= 1)
            {
                return center;
            }

            var angle = 2f * Mathf.PI * index / count;
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private readonly struct PendingSplit
        {
            public readonly SplitOnDeath Split;
            public readonly Vector2 Position;
            public readonly float ParentRadius;
            public readonly Entity ExperienceRecipient;

            public PendingSplit(
                SplitOnDeath split,
                Vector2 position,
                float parentRadius,
                Entity experienceRecipient)
            {
                Split = split;
                Position = position;
                ParentRadius = parentRadius;
                ExperienceRecipient = experienceRecipient;
            }
        }
    }
}
