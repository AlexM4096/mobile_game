using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;

namespace _Project.Gameplay.Features.Spear.Systems
{
    public sealed class FireSpearProjectilesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<SpearShotRequest>();

        private readonly SpearConfig _settings;
        private readonly SpearFactory _factory;
        private readonly List<Entity> _requestsToDestroy = new();
        private readonly List<SpearShotRequest> _requestsToFire = new();

        public FireSpearProjectilesSystem(
            World world,
            SpearConfig settings,
            SpearFactory factory) : base(world)
        {
            _settings = settings;
            _factory = factory;
        }

        public override void Update(in SystemState state)
        {
            _requestsToDestroy.Clear();
            _requestsToFire.Clear();
            var canFire = _settings.Enabled && _settings.TryValidate(out _);
            var currentTime = state.Time;

            World.Query(in _description, (Entity entity, ref SpearShotRequest request) =>
            {
                if (!canFire)
                {
                    _requestsToDestroy.Add(entity);
                    return;
                }

                if (currentTime < request.FireTime)
                {
                    return;
                }

                _requestsToDestroy.Add(entity);
                _requestsToFire.Add(request);
            });

            for (var requestIndex = 0; requestIndex < _requestsToFire.Count; requestIndex++)
            {
                var request = _requestsToFire[requestIndex];
                if (!World.IsAlive(request.Owner) || !World.Has<Position>(request.Owner))
                {
                    continue;
                }

                var origin = World.Get<Position>(request.Owner).Value;
                _factory.Create(
                    origin,
                    request.Direction,
                    request.DamageHits,
                    request.TotalHealth);
            }

            for (var requestIndex = 0; requestIndex < _requestsToDestroy.Count; requestIndex++)
            {
                var request = _requestsToDestroy[requestIndex];
                if (World.IsAlive(request))
                {
                    World.Destroy(request);
                }
            }
        }
    }
}
