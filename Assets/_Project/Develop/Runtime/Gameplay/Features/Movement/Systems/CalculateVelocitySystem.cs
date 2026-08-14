using Arch.Core;
using Arch.Unity.Jobs;
using Arch.Unity.Toolkit;
using Unity.Burst;

namespace _Project.Gameplay.Features.Movement.Systems
{
    /// <summary>Calculates linear velocity from movement direction and speed.</summary>
    public sealed class CalculateVelocitySystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Direction, MoveSpeed, Velocity>();

        public CalculateVelocitySystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            var job = new CalculateVelocityJob
            {
                DirectionId = Component<Direction>.ComponentType.Id,
                MoveSpeedId = Component<MoveSpeed>.ComponentType.Id,
                VelocityId = Component<Velocity>.ComponentType.Id
            };

            job.ScheduleParallel(World, _description).Complete();
        }

        [BurstCompile]
        private struct CalculateVelocityJob : IJobArchChunk
        {
            public int DirectionId;
            public int MoveSpeedId;
            public int VelocityId;

            public void Execute(NativeChunk chunk)
            {
                var directions = chunk.GetNativeArray<Direction>(DirectionId);
                var moveSpeeds = chunk.GetNativeArray<MoveSpeed>(MoveSpeedId);
                var velocities = chunk.GetNativeArray<Velocity>(VelocityId);

                for (var index = 0; index < velocities.Length; index++)
                {
                    var velocity = velocities[index];
                    velocity.Value = directions[index].Value * moveSpeeds[index].Value;
                    velocities[index] = velocity;
                }
            }
        }
    }
}
