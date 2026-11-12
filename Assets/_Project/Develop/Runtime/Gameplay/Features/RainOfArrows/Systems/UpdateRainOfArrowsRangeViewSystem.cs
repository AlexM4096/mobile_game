using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Player;
using UnityEngine;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class UpdateRainOfArrowsRangeViewSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, Position, RainOfArrowsAbility>()
                .WithNone<DeadTag>();

        private readonly RainOfArrowsFactory _factory;
        private LineRenderer _rangeLine;

        public UpdateRainOfArrowsRangeViewSystem(
            World world,
            RainOfArrowsFactory factory) : base(world)
        {
            _factory = factory;
        }

        public override void Initialize()
        {
            _rangeLine = _factory.CreateTargetingRangeRenderer();
        }

        public override void Update(in SystemState state)
        {
            var found = false;
            World.Query(in _description, (
                ref Position position,
                ref RainOfArrowsAbility ability) =>
            {
                if (found)
                {
                    return;
                }

                found = true;
                _factory.UpdateCircle(
                    _rangeLine,
                    position.Value,
                    ability.TargetingRange);
            });

            if (_rangeLine != null)
            {
                _rangeLine.enabled = found;
            }
        }
    }
}
