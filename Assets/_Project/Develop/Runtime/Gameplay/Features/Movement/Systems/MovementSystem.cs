using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement.Components;
using Arch.Unity.Jobs;
using Unity.Burst;

namespace _Project.Gameplay.Features.Movement.Systems
{
    public sealed class MovementSystem : UnitySystemBase
    {
        private readonly QueryDescription query = 
            new QueryDescription()
                .WithAll<Position, Velocity>();

        public MovementSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            var job = new PositionUpdateJob()
            {
                DeltaTime = state.DeltaTime,
                PositionId = Component<Position>.ComponentType.Id,
                VelocityId = Component<Velocity>.ComponentType.Id,
            };
            job.ScheduleParallel(World, query).Complete();
        }

        [BurstCompile]
        public struct PositionUpdateJob : IJobArchChunk
        {
            public float DeltaTime;

            public int PositionId;
            public int VelocityId;

            public void Execute(NativeChunk chunk)
            {
                var positionArray = chunk.GetNativeArray<Position>(PositionId);
                var velocityArray = chunk.GetNativeArray<Velocity>(VelocityId);

                var length = positionArray.Length;

                for (int i = 0; i < length; i++)
                {
                    var position = positionArray[i];
                    position.Value += velocityArray[i].Value * DeltaTime;
                    positionArray[i] = position;
                }
            }
        }
    }
}
