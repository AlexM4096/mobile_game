using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Death;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class ApplyEnemyHitsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<HitRequest, EnemyDefense>()
                .WithNone<DeadTag>();

        public ApplyEnemyHitsSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (
                Entity entity,
                ref HitRequest hitRequest,
                ref EnemyDefense defense) =>
            {
                var hitResult = EnemyDefenseRules.ApplyHits(ref defense, hitRequest.Count);

                if (hitRequest.Count > 0 && World.Has<GameObjectReference>(entity))
                {
                    if (World.Has<DefenseViewUpdate>(entity))
                    {
                        ref var viewUpdate = ref World.Get<DefenseViewUpdate>(entity);
                        viewUpdate.HelmetDurability = defense.HelmetDurability;
                        viewUpdate.BrokenLayers |= hitResult.BrokenLayers;
                    }
                    else
                    {
                        commandBuffer.Add(entity, new DefenseViewUpdate
                        {
                            HelmetDurability = defense.HelmetDurability,
                            BrokenLayers = hitResult.BrokenLayers
                        });
                    }
                }

                if (hitResult.IsDefeated)
                {
                    commandBuffer.Add<DeadTag>(entity);
                }

                commandBuffer.Remove<HitRequest>(entity);
            });

            commandBuffer.Playback(World, false);
        }
    }
}