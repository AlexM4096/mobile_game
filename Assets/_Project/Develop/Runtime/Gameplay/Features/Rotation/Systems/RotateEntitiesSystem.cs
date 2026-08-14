using Arch.Core;
using Arch.Unity.Jobs;
using Arch.Unity.Toolkit;
using Unity.Burst;
using Unity.Mathematics;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Rotation.Systems
{
    /// <summary>Integrates entity rotation from world-space angular velocity in radians per second.</summary>
    public sealed class RotateEntitiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<RotationComponent, AngularVelocity>();

        public RotateEntitiesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            var job = new RotateEntitiesJob
            {
                DeltaTime = state.DeltaTime,
                RotationId = Component<RotationComponent>.ComponentType.Id,
                AngularVelocityId = Component<AngularVelocity>.ComponentType.Id
            };

            job.ScheduleParallel(World, _description).Complete();
        }

        [BurstCompile]
        private struct RotateEntitiesJob : IJobArchChunk
        {
            public float DeltaTime;
            public int RotationId;
            public int AngularVelocityId;

            public void Execute(NativeChunk chunk)
            {
                var rotations = chunk.GetNativeArray<RotationComponent>(RotationId);
                var angularVelocities = chunk.GetNativeArray<AngularVelocity>(AngularVelocityId);

                for (var index = 0; index < rotations.Length; index++)
                {
                    var angularVelocity = angularVelocities[index].Value;

                    var angularSpeed = math.length(angularVelocity);
                    if (angularSpeed <= math.EPSILON) continue;

                    var rotation = rotations[index];
                    var deltaRotation = quaternion.AxisAngle(
                        angularVelocity / angularSpeed,
                        angularSpeed * DeltaTime
                    );
                    rotation.Value = math.mul(deltaRotation, rotation.Value);
                    rotations[index] = rotation;
                }
            }
        }
    }
}
