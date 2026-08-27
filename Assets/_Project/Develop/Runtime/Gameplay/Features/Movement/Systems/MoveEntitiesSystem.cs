using Arch.Core;
using Arch.Unity.Jobs;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using Unity.Burst;

namespace _Project.Gameplay.Features.Movement.Systems
{
    /// <summary>Integrates entity positions from their velocities.</summary>
    public sealed class MoveEntitiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, Velocity>()
                .WithNone<ManualMovementTag>();

        public MoveEntitiesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            var job = new MoveEntitiesJob
            {
                DeltaTime = state.DeltaTime,
                PositionId = Component<Position>.ComponentType.Id,
                VelocityId = Component<Velocity>.ComponentType.Id
            };

            job.ScheduleParallel(World, _description).Complete();
        }

        [BurstCompile]
        private struct MoveEntitiesJob : IJobArchChunk
        {
            public float DeltaTime;
            public int PositionId;
            public int VelocityId;

            public void Execute(NativeChunk chunk)
            {
                var positions = chunk.GetNativeArray<Position>(PositionId);
                var velocities = chunk.GetNativeArray<Velocity>(VelocityId);

                for (var index = 0; index < positions.Length; index++)
                {
                    var position = positions[index];
                    position.Value += velocities[index].Value * DeltaTime;
                    positions[index] = position;
                }
            }
        }
    }
}
